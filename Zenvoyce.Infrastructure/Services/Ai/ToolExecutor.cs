using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Zenvoyce.Infrastructure.Services.Ai;

/// <summary>
/// Thực thi các tool được model yêu cầu — query trực tiếp vào PostgreSQL bằng Dapper.
/// Mỗi method tương ứng với một tool trong VertexAiTools.Definitions.
/// </summary>
public sealed class ToolExecutor
{
    private readonly string _connectionString;

    public ToolExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    /// <summary>
    /// Điều phối thực thi tool theo tên.
    /// </summary>
    public async Task<object> ExecuteAsync(string toolName, JsonElement args)
    {
        try
        {
            return toolName switch
            {
                "get_invoice_summary"    => await GetInvoiceSummaryAsync(args),
                "get_customer_invoices"  => await GetCustomerInvoicesAsync(args),
                "get_invoices_by_status" => await GetInvoicesByStatusAsync(args),
                "get_invoice_detail"     => await GetInvoiceDetailAsync(args),
                _                        => throw new NotSupportedException($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex) when (ex is not NotSupportedException)
        {
            // Không để exception crash agentic loop — trả về error message để model biết
            return new { error = ex.Message, tool = toolName };
        }
    }

    // ─── Tool 1: Thống kê hóa đơn theo tháng/năm ────────────────────────────

    private async Task<object> GetInvoiceSummaryAsync(JsonElement args)
    {
        var year      = args.TryGetProperty("year",       out var y) ? y.GetInt32()     : DateTime.Now.Year;
        var month     = args.TryGetProperty("month",      out var m) ? (int?)m.GetInt32() : null;
        var companyId = args.TryGetProperty("company_id", out var c) ? c.GetString()    : null;

        var sql = @"
            SELECT
                COUNT(*)                    AS total_invoices,
                COALESCE(SUM(""Tongtien""),        0) AS total_amount,
                COALESCE(SUM(""TienThue""),        0) AS total_tax,
                COALESCE(SUM(""TongThanhToan""),   0) AS total_payment,
                ""Trangthai""                         AS status,
                COUNT(*) FILTER (WHERE ""Trangthai"" = 'Signed')    AS signed_count,
                COUNT(*) FILTER (WHERE ""Trangthai"" = 'Issued')    AS issued_count,
                COUNT(*) FILTER (WHERE ""Trangthai"" = 'Draft')     AS draft_count,
                COUNT(*) FILTER (WHERE ""Trangthai"" = 'Cancelled') AS cancelled_count
            FROM ""TTHoadon""
            WHERE EXTRACT(YEAR FROM ""Ngaylap"") = @year
              AND (@month   IS NULL OR EXTRACT(MONTH FROM ""Ngaylap"") = @month)
              AND (@companyId IS NULL OR ""DonviID""::text = @companyId)
              AND ""Is_Deleted"" = FALSE
            GROUP BY ""Trangthai""
            ORDER BY ""Trangthai""";

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync(sql, new { year, month, companyId });

        return new
        {
            year,
            month     = month ?? 0,
            company_id = companyId ?? "ALL",
            breakdown  = rows
        };
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
                hd.""ID""           AS invoice_id,
                hd.""SoHoadon""     AS invoice_number,
                hd.""Kyhieu""       AS series,
                hd.""Ngaylap""      AS issue_date,
                hd.""Tongtien""     AS amount,
                hd.""TienThue""     AS tax_amount,
                hd.""TongThanhToan"" AS total_payment,
                hd.""Trangthai""    AS status,
                kh.""Tenkhachhang"" AS customer_name,
                kh.""MasoThue""     AS customer_tax_code
            FROM   ""TTHoadon""     hd
            JOIN   ""TTkhachhang""  kh ON kh.""ID"" = hd.""KhachhangID""
            WHERE  hd.""Is_Deleted"" = FALSE
              AND  (@customerName IS NULL OR kh.""Tenkhachhang"" ILIKE '%' || @customerName || '%')
              AND  (@taxCode IS NULL OR kh.""MasoThue"" = @taxCode)
            ORDER  BY hd.""Ngaylap"" DESC
            LIMIT  @limit";

        await using var conn = new NpgsqlConnection(_connectionString);
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
                hd.""ID""            AS invoice_id,
                hd.""SoHoadon""      AS invoice_number,
                hd.""Kyhieu""        AS series,
                hd.""Ngaylap""       AS issue_date,
                hd.""TongThanhToan"" AS total_payment,
                hd.""Trangthai""     AS status,
                kh.""Tenkhachhang""  AS customer_name,
                kh.""MasoThue""      AS customer_tax_code,
                cty.""Tendonvi""     AS company_name
            FROM  ""TTHoadon""     hd
            LEFT JOIN ""TTkhachhang"" kh  ON kh.""ID""  = hd.""KhachhangID""
            LEFT JOIN ""TTcty""       cty ON cty.""ID"" = hd.""DonviID""
            WHERE hd.""Trangthai""   = @status
              AND hd.""Is_Deleted""  = FALSE
              AND (@year      IS NULL OR EXTRACT(YEAR  FROM hd.""Ngaylap"") = @year)
              AND (@month     IS NULL OR EXTRACT(MONTH FROM hd.""Ngaylap"") = @month)
              AND (@companyId IS NULL OR hd.""DonviID""::text = @companyId)
            ORDER BY hd.""Ngaylap"" DESC
            LIMIT @limit";

        await using var conn = new NpgsqlConnection(_connectionString);
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
                hd.""ID""            AS invoice_id,
                hd.""SoHoadon""      AS invoice_number,
                hd.""Kyhieu""        AS series,
                hd.""Ngaylap""       AS issue_date,
                hd.""Tongtien""      AS amount,
                hd.""TienThue""      AS tax_amount,
                hd.""TongThanhToan"" AS total_payment,
                hd.""Trangthai""     AS status,
                kh.""Tenkhachhang""  AS customer_name,
                kh.""MasoThue""      AS customer_tax_code,
                kh.""Email""         AS customer_email,
                cty.""Tendonvi""     AS company_name,
                cty.""MasoThue""     AS company_tax_code
            FROM  ""TTHoadon""     hd
            LEFT JOIN ""TTkhachhang"" kh  ON kh.""ID""  = hd.""KhachhangID""
            LEFT JOIN ""TTcty""       cty ON cty.""ID"" = hd.""DonviID""
            WHERE hd.""Is_Deleted"" = FALSE
              AND (@invoiceNumber IS NULL OR hd.""SoHoadon"" = @invoiceNumber)
              AND (@series        IS NULL OR hd.""Kyhieu""   = @series)
            LIMIT 1";

        var lineItemSql = @"
            SELECT
                hh.""Soluong""   AS quantity,
                hh.""Dongia""    AS unit_price,
                hh.""ThueSuat""  AS tax_rate,
                hh.""Thanhtien"" AS line_total,
                dm.""Tenhanghoa"" AS product_name,
                dm.""Donvitinh"" AS unit
            FROM  ""TTHangHoa""        hh
            LEFT JOIN ""Danhmuchanghoa"" dm ON dm.""ID"" = hh.""HanghoaID""
            WHERE hh.""HoadonID"" = @invoiceId";

        await using var conn = new NpgsqlConnection(_connectionString);
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
}
