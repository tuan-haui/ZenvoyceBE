using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Zenvoyce.Infrastructure.Services.Ai;

/// <summary>
/// Thực thi các tool được model yêu cầu — query trực tiếp vào PostgreSQL bằng Dapper.
/// Mỗi method tương ứng với một tool trong VertexAiTools.Definitions.
/// </summary>
public sealed class ToolExecutor
{
    private readonly string           _connectionString;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(IConfiguration configuration, ILogger<ToolExecutor> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' không được cấu hình trong appsettings.json");
        
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("[ToolExecutor] Initialized with connection string (Host: {Host})", 
            ExtractHostFromConnectionString(_connectionString));
    }

    /// <summary>
    /// Điều phối thực thi tool theo tên.
    /// </summary>
    public async Task<object> ExecuteAsync(string toolName, JsonElement args)
    {
        try
        {
            _logger.LogInformation("[ToolExecutor] Executing tool: {ToolName} with args: {Args}", 
                toolName, args.GetRawText());
            
            var result = toolName switch
            {
                "get_invoice_summary"    => await GetInvoiceSummaryAsync(args),
                "get_customer_invoices"  => await GetCustomerInvoicesAsync(args),
                "get_invoices_by_status" => await GetInvoicesByStatusAsync(args),
                "get_invoice_detail"              => await GetInvoiceDetailAsync(args),
                "get_invoices_for_risk_assessment" => await GetInvoicesForRiskAssessmentAsync(args),
                _                                 => throw new NotSupportedException($"Tool không được hỗ trợ: {toolName}")
            };
            
            _logger.LogInformation("[ToolExecutor] Tool {ToolName} executed successfully", toolName);
            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Lỗi thực thi tool '{toolName}': {ex.Message}";
            _logger.LogError(ex, "[ToolExecutor] Tool execution failed: {ErrorMessage}", errorMessage);
            
            // Không để exception crash agentic loop — trả về error message để model biết
            return new { error = errorMessage, tool = toolName, exceptionType = ex.GetType().Name };
        }
    }

    // ─── Tool 1: Thống kê hóa đơn theo tháng/năm ────────────────────────────

    private async Task<object> GetInvoiceSummaryAsync(JsonElement args)
    {
        try
        {
            var year      = args.TryGetProperty("year",       out var y) ? y.GetInt32()     : DateTime.Now.Year;
            var month     = args.TryGetProperty("month",      out var m) ? (int?)m.GetInt32() : null;
            var companyId = args.TryGetProperty("company_id", out var c) ? c.GetString()    : null;

            var sql = @"
                SELECT
                    COUNT(*)                    AS total_invoices,
                    COALESCE(SUM(""tongtien""),        0) AS total_amount,
                    COALESCE(SUM(""tienthue""),        0) AS total_tax,
                    COALESCE(SUM(""tongthanhtoan""),   0) AS total_payment,
                    ""trangthai""                         AS status,
                    COUNT(*) FILTER (WHERE ""trangthai"" = 'Signed')    AS signed_count,
                    COUNT(*) FILTER (WHERE ""trangthai"" = 'Issued')    AS issued_count,
                    COUNT(*) FILTER (WHERE ""trangthai"" = 'Draft')     AS draft_count,
                    COUNT(*) FILTER (WHERE ""trangthai"" = 'Cancelled') AS cancelled_count
                FROM ""tthoadon""
                WHERE EXTRACT(YEAR FROM ""ngaylap"") = @year
                  AND (@month   IS NULL OR EXTRACT(MONTH FROM ""ngaylap"") = @month)
                  AND (@companyId IS NULL OR ""donviid""::text = @companyId)
                  AND ""is_deleted"" = FALSE
                GROUP BY ""trangthai""
                ORDER BY ""trangthai""";

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var rows = await conn.QueryAsync(sql, new { year, month, companyId });

            return new
            {
                year,
                month     = month ?? 0,
                company_id = companyId ?? "ALL",
                breakdown  = rows
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetInvoiceSummaryAsync] Failed with error");
            throw;
        }
    }

    // ─── Tool 2: Hóa đơn của khách hàng ─────────────────────────────────────

    private async Task<object> GetCustomerInvoicesAsync(JsonElement args)
    {
        var customerName = args.TryGetProperty("customer_name", out var cn) ? cn.GetString() : null;
        var taxCode      = args.TryGetProperty("tax_code",      out var tc) ? tc.GetString() : null;
        var limit        = args.TryGetProperty("limit",         out var l)  ? Math.Min(l.GetInt32(), 50) : 10;

        if (string.IsNullOrWhiteSpace(customerName) && string.IsNullOrWhiteSpace(taxCode))
            return new { error = "Cần cung cấp ít nhất customer_name hoặc tax_code." };

        var sql = @"
            SELECT
                hd.""id""           AS invoice_id,
                hd.""sohoadon""     AS invoice_number,
                hd.""kyhieu""       AS series,
                hd.""ngaylap""      AS issue_date,
                hd.""tongtien""     AS amount,
                hd.""tienthue""     AS tax_amount,
                hd.""tongthanhtoan"" AS total_payment,
                hd.""trangthai""    AS status,
                kh.""tenkhachhang"" AS customer_name,
                kh.""masothue""     AS customer_tax_code
            FROM   ""tthoadon""     hd
            JOIN   ""ttkhachhang""  kh ON kh.""id"" = hd.""khachhangid""
            WHERE  hd.""is_deleted"" = FALSE
              AND  (@customerName IS NULL OR kh.""tenkhachhang"" ILIKE '%' || @customerName || '%')
              AND  (@taxCode IS NULL OR kh.""masothue"" = @taxCode)
            ORDER  BY hd.""ngaylap"" DESC
            LIMIT  @limit";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var invoices = await conn.QueryAsync(sql, new { customerName, taxCode, limit });
        var list     = invoices.ToList();

        return new
        {
            customer_name = customerName,
            tax_code      = taxCode,
            total_found   = list.Count,
            invoices      = list
        };
    }

    // ─── Tool 3: Hóa đơn theo trạng thái ────────────────────────────────────

    private async Task<object> GetInvoicesByStatusAsync(JsonElement args)
    {
        var status    = args.GetProperty("status").GetString()!;
        var year      = args.TryGetProperty("year",      out var y) ? (int?)y.GetInt32()   : null;
        var month     = args.TryGetProperty("month",     out var m) ? (int?)m.GetInt32()   : null;
        var companyId = args.TryGetProperty("company_id",out var c) ? c.GetString()        : null;
        var limit     = args.TryGetProperty("limit",     out var l) ? Math.Min(l.GetInt32(), 100) : 20;

        var sql = @"
            SELECT
                hd.""id""            AS invoice_id,
                hd.""sohoadon""      AS invoice_number,
                hd.""kyhieu""        AS series,
                hd.""ngaylap""       AS issue_date,
                hd.""tongthanhtoan"" AS total_payment,
                hd.""trangthai""     AS status,
                kh.""tenkhachhang""  AS customer_name,
                kh.""masothue""      AS customer_tax_code,
                cty.""tendonvi""     AS company_name
            FROM  ""tthoadon""     hd
            LEFT JOIN ""ttkhachhang"" kh  ON kh.""id""  = hd.""khachhangid""
            LEFT JOIN ""ttcty""       cty ON cty.""id"" = hd.""donviid""
            WHERE hd.""trangthai""   = @status
              AND hd.""is_deleted""  = FALSE
              AND (@year      IS NULL OR EXTRACT(YEAR  FROM hd.""ngaylap"") = @year)
              AND (@month     IS NULL OR EXTRACT(MONTH FROM hd.""ngaylap"") = @month)
              AND (@companyId IS NULL OR hd.""donviid""::text = @companyId)
            ORDER BY hd.""ngaylap"" DESC
            LIMIT @limit";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var rows = await conn.QueryAsync(sql, new { status, year, month, companyId, limit });
        var list = rows.ToList();

        return new
        {
            status,
            year,
            month,
            total_found = list.Count,
            invoices    = list
        };
    }

    // ─── Tool 4: Chi tiết hóa đơn ───────────────────────────────────────────

    private async Task<object> GetInvoiceDetailAsync(JsonElement args)
    {
        var invoiceNumber = args.TryGetProperty("invoice_number", out var n) ? n.GetString() : null;
        var series        = args.TryGetProperty("series",         out var s) ? s.GetString() : null;

        if (string.IsNullOrWhiteSpace(invoiceNumber) && string.IsNullOrWhiteSpace(series))
            return new { error = "Cần cung cấp ít nhất invoice_number hoặc series." };

        var sql = @"
            SELECT
                hd.""id""            AS invoice_id,
                hd.""sohoadon""      AS invoice_number,
                hd.""kyhieu""        AS series,
                hd.""ngaylap""       AS issue_date,
                hd.""tongtien""      AS amount,
                hd.""tienthue""      AS tax_amount,
                hd.""tongthanhtoan"" AS total_payment,
                hd.""trangthai""     AS status,
                kh.""tenkhachhang""  AS customer_name,
                kh.""masothue""      AS customer_tax_code,
                kh.""email""         AS customer_email,
                cty.""tendonvi""     AS company_name,
                cty.""masothue""     AS company_tax_code
            FROM  ""tthoadon""     hd
            LEFT JOIN ""ttkhachhang"" kh  ON kh.""id""  = hd.""khachhangid""
            LEFT JOIN ""ttcty""       cty ON cty.""id"" = hd.""donviid""
            WHERE hd.""is_deleted"" = FALSE
              AND (@invoiceNumber IS NULL OR hd.""sohoadon"" = @invoiceNumber)
              AND (@series        IS NULL OR hd.""kyhieu""   = @series)
            LIMIT 1";

        var lineItemSql = @"
            SELECT
                hh.""soluong""   AS quantity,
                hh.""dongia""    AS unit_price,
                hh.""thuesuat""  AS tax_rate,
                hh.""thanhtien"" AS line_total,
                dm.""tenhanghoa"" AS product_name,
                dm.""donvitinh"" AS unit
            FROM  ""hoadonchitiet""        hh
            LEFT JOIN ""danhmuchanghoa"" dm ON dm.""id"" = hh.""hanghoaid""
            WHERE hh.""hoadonid"" = @invoiceId";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var invoice  = await conn.QueryFirstOrDefaultAsync(sql, new { invoiceNumber, series });

        if (invoice is null)
            return new { error = "Không tìm thấy hóa đơn.", invoice_number = invoiceNumber, series };

        var invoiceId = (Guid)invoice.invoice_id;
        var lineItems = await conn.QueryAsync(lineItemSql, new { invoiceId });

        return new
        {
            invoice,
            line_items = lineItems.ToList()
        };
    }

    // ─── Tool 5: Lấy batch hóa đơn để AI đánh giá rủi ro ───────────────────

    private async Task<object> GetInvoicesForRiskAssessmentAsync(JsonElement args)
    {
        var limit = args.TryGetProperty("limit", out var l)
            ? Math.Clamp(l.GetInt32(), 1, 50)
            : 20;
        var year      = args.TryGetProperty("year",       out var y) ? (int?)y.GetInt32() : null;
        var month     = args.TryGetProperty("month",      out var m) ? (int?)m.GetInt32() : null;
        var companyId = args.TryGetProperty("company_id", out var c) ? c.GetString()       : null;
        var status    = args.TryGetProperty("status",     out var s) ? s.GetString()       : null;

        var sql = @"
            SELECT
                hd.""id""              AS invoice_id,
                hd.""sohoadon""        AS invoice_number,
                hd.""kyhieu""          AS series,
                hd.""ngaylap""         AS issue_date,
                hd.""tongtien""        AS amount,
                hd.""tienthue""        AS tax_amount,
                hd.""tongthanhtoan""   AS total_payment,
                hd.""trangthai""       AS status,
                (hd.""xmldaky"" IS NOT NULL AND TRIM(hd.""xmldaky"") <> '') AS has_signed_xml,
                (hd.""thamchieuhoadonid"" IS NOT NULL) AS has_reference_invoice,
                EXTRACT(DAY FROM (NOW() - hd.""ngaylap""))::int AS days_since_issue,
                CASE
                    WHEN COALESCE(hd.""tongtien"", 0) > 0
                    THEN ROUND((hd.""tienthue"" / hd.""tongtien"") * 100, 2)
                    ELSE 0
                END AS tax_ratio_percent,
                kh.""tenkhachhang""    AS customer_name,
                kh.""masothue""        AS customer_tax_code,
                kh.""email""           AS customer_email,
                kh.""dienthoai""       AS customer_phone,
                cty.""tendonvi""       AS company_name,
                cty.""masothue""       AS company_tax_code,
                cust_stats.invoice_count AS customer_invoice_count,
                COALESCE(hist.history_count, 0) AS history_event_count,
                COALESCE(lines.line_item_count, 0) AS line_item_count,
                COALESCE(lines.line_items_total, 0) AS line_items_total,
                COALESCE(lines.max_line_amount, 0) AS max_line_amount,
                COALESCE(lines.distinct_product_count, 0) AS distinct_product_count,
                COALESCE(sig.has_signature, FALSE) AS has_digital_signature
            FROM  ""tthoadon"" hd
            LEFT JOIN ""ttkhachhang"" kh  ON kh.""id""  = hd.""khachhangid""
            LEFT JOIN ""ttcty""       cty ON cty.""id"" = hd.""donviid""
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS invoice_count
                FROM   ""tthoadon"" x
                WHERE  x.""khachhangid"" = hd.""khachhangid""
                  AND  x.""is_deleted"" = FALSE
            ) cust_stats ON TRUE
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS history_count
                FROM   ""lichsuhoadon"" ls
                WHERE  ls.""hoadonid"" = hd.""id""
            ) hist ON TRUE
            LEFT JOIN LATERAL (
                SELECT
                    COUNT(*)::int                    AS line_item_count,
                    COALESCE(SUM(hh.""thanhtien""), 0) AS line_items_total,
                    COALESCE(MAX(hh.""thanhtien""), 0) AS max_line_amount,
                    COUNT(DISTINCT hh.""hanghoaid"")::int AS distinct_product_count
                FROM   ""hoadonchitiet"" hh
                WHERE  hh.""hoadonid"" = hd.""id""
            ) lines ON TRUE
            LEFT JOIN LATERAL (
                SELECT EXISTS(
                    SELECT 1 FROM ""qlkyso"" ks WHERE ks.""hoadonid"" = hd.""id""
                ) AS has_signature
            ) sig ON TRUE
            WHERE hd.""is_deleted"" = FALSE
              AND (@year      IS NULL OR EXTRACT(YEAR  FROM hd.""ngaylap"") = @year)
              AND (@month     IS NULL OR EXTRACT(MONTH FROM hd.""ngaylap"") = @month)
              AND (@companyId IS NULL OR hd.""donviid""::text = @companyId)
              AND (@status    IS NULL OR hd.""trangthai"" = @status)
            ORDER BY hd.""ngaylap"" DESC
            LIMIT  @limit";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var rows = await conn.QueryAsync(sql, new { limit, year, month, companyId, status });
        var list = rows.ToList();

        var summary = new
        {
            total_returned     = list.Count,
            draft_count        = list.Count(r => string.Equals((string?)r.status, "Draft",     StringComparison.OrdinalIgnoreCase)),
            signed_count       = list.Count(r => string.Equals((string?)r.status, "Signed",    StringComparison.OrdinalIgnoreCase)),
            issued_count       = list.Count(r => string.Equals((string?)r.status, "Issued",    StringComparison.OrdinalIgnoreCase)),
            cancelled_count    = list.Count(r => string.Equals((string?)r.status, "Cancelled", StringComparison.OrdinalIgnoreCase)),
            without_tax_code   = list.Count(r => string.IsNullOrWhiteSpace((string?)r.customer_tax_code)),
            high_amount_count  = list.Count(r => r.total_payment is decimal p && p >= 100_000_000),
            unsigned_xml_count = list.Count(r => r.has_signed_xml is not true && r.has_digital_signature is not true),
            avg_total_payment  = list.Count > 0
                ? Math.Round(list.Average(r => r.total_payment is decimal p ? (double)p : 0d), 2)
                : 0d
        };

        return new
        {
            purpose = "risk_assessment",
            limit,
            filters = new { year, month, company_id = companyId ?? "ALL", status = status ?? "ALL" },
            summary,
            risk_signals_hint = new[]
            {
                "Hóa đơn Draft lâu ngày (days_since_issue cao) có thể chưa hoàn tất quy trình.",
                "Khách hàng thiếu mã số thuế (customer_tax_code rỗng) tăng rủi ro tuân thủ.",
                "Tỷ lệ thuế bất thường (tax_ratio_percent) so với mức chuẩn 8-10%.",
                "Hóa đơn có tham chiếu (has_reference_invoice) có thể là điều chỉnh/thay thế.",
                "Nhiều sự kiện lịch sử (history_event_count) gợi ý thay đổi/tranh chấp.",
                "Giá trị cao (total_payment) cần đối chiếu dòng hàng (line_items_total, max_line_amount)."
            },
            invoices = list
        };
    }

    // ─── Helper: Extract host from connection string for logging ─────────────
    
    private static string ExtractHostFromConnectionString(string connectionString)
    {
        try
        {
            var host = connectionString.Split(';')
                .FirstOrDefault(s => s.StartsWith("Host="))?
                .Replace("Host=", "");
            return host ?? "unknown";
        }
        catch { return "unknown"; }
    }
}
