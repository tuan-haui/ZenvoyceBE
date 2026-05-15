namespace Zenvoyce.Infrastructure.Services.Ai;

/// <summary>
/// Khai báo danh sách tools (function declarations) gửi lên Vertex AI.
/// Model sẽ dựa vào description để quyết định khi nào gọi tool nào.
///
/// Schema DB Zenvoyce:
///   TTHoadon      — bảng hóa đơn
///   TTkhachhang   — bảng khách hàng
///   TTcty         — bảng công ty / đơn vị
///   TTHangHoa     — dòng hàng hóa trong hóa đơn
///   Danhmuchanghoa — danh mục hàng hóa
/// </summary>
public static class VertexAiTools
{
    public static object[] Definitions => new[]
    {
        new
        {
            functionDeclarations = new object[]
            {
                // ── Tool 1: Thống kê hóa đơn ──────────────────────────────────
                new
                {
                    name = "get_invoice_summary",
                    description =
                        "Lấy thống kê tổng hợp hóa đơn theo tháng và năm: tổng số hóa đơn, tổng tiền, tổng tiền thuế, tổng thanh toán. Dùng khi người dùng hỏi về doanh thu, số lượng hóa đơn, tổng tiền trong kỳ.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            month = new { type = "integer", description = "Tháng (1-12). Bỏ trống để lấy cả năm." },
                            year = new { type = "integer", description = "Năm (ví dụ: 2025, 2026)" },
                            company_id = new
                            {
                                type = "string", description = "ID UUID của công ty/đơn vị. Bỏ trống = tất cả công ty."
                            }
                        },
                        required = new[] { "year" }
                    }
                },

                // ── Tool 2: Hóa đơn của khách hàng ────────────────────────────
                new
                {
                    name = "get_customer_invoices",
                    description =
                        "Lấy danh sách hóa đơn của một khách hàng cụ thể, kèm tổng tiền và trạng thái. Dùng khi người dùng hỏi về hóa đơn của một khách hàng theo tên hoặc mã số thuế.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            customer_name = new
                                { type = "string", description = "Tên hoặc một phần tên khách hàng (tìm kiếm LIKE)" },
                            tax_code = new
                                { type = "string", description = "Mã số thuế của khách hàng (tìm kiếm chính xác)" },
                            limit = new
                            {
                                type = "integer",
                                description = "Số lượng hóa đơn tối đa trả về (mặc định 10, tối đa 50)"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                },

                // ── Tool 3: Hóa đơn theo trạng thái ───────────────────────────
                new
                {
                    name = "get_invoices_by_status",
                    description =
                        "Lấy danh sách hóa đơn lọc theo trạng thái. Trạng thái hợp lệ: 'Draft' (nháp), 'Signed' (đã ký), 'Issued' (đã phát hành), 'Cancelled' (đã hủy). Dùng khi người dùng hỏi về hóa đơn đã ký, chưa ký, bị hủy, v.v.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string", description = "Trạng thái hóa đơn: Draft | Signed | Issued | Cancelled"
                            },
                            year = new { type = "integer", description = "Lọc theo năm (tuỳ chọn)" },
                            month = new { type = "integer", description = "Lọc theo tháng (tuỳ chọn, dùng kèm year)" },
                            company_id = new { type = "string", description = "ID UUID công ty (tuỳ chọn)" },
                            limit = new { type = "integer", description = "Số lượng tối đa trả về (mặc định 20)" }
                        },
                        required = new[] { "status" }
                    }
                },

                // ── Tool 4: Tìm kiếm hóa đơn theo số hóa đơn ─────────────────
                new
                {
                    name = "get_invoice_detail",
                    description =
                        "Lấy chi tiết một hóa đơn cụ thể theo số hóa đơn (SoHoadon) hoặc ký hiệu (Kyhieu). Trả về đầy đủ thông tin hóa đơn gồm khách hàng, các dòng hàng hóa, tiền thuế.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            invoice_number = new
                                { type = "string", description = "Số hóa đơn (SoHoadon), ví dụ: 'HD01-01012026'" },
                            series = new { type = "string", description = "Ký hiệu hóa đơn (Kyhieu), ví dụ: 'AA/25E'" }
                        },
                        required = Array.Empty<string>()
                    }
                },

                // ── Tool 5: Batch hóa đơn cho đánh giá rủi ro ─────────────────
                new
                {
                    name = "get_invoices_for_risk_assessment",
                    description =
                        "Lấy một tập hóa đơn gần đây (theo số lượng limit) kèm chỉ số phục vụ đánh giá rủi ro: trạng thái, XML ký, tỷ lệ thuế, lịch sử thay đổi, dòng hàng, khách hàng thiếu MST, v.v. Dùng khi người dùng yêu cầu phân tích/đánh giá rủi ro hóa đơn, kiểm tra bất thường, hoặc rà soát tuân thủ.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            limit = new
                            {
                                type = "integer", description = "Số lượng hóa đơn cần lấy (1-50, mặc định 20)"
                            },
                            year = new { type = "integer", description = "Lọc theo năm lập hóa đơn (tuỳ chọn)" },
                            month = new
                            {
                                type = "integer", description = "Lọc theo tháng lập hóa đơn (tuỳ chọn, dùng kèm year)"
                            },
                            company_id = new { type = "string", description = "ID UUID công ty/đơn vị (tuỳ chọn)" },
                            status = new
                            {
                                type = "string",
                                description = "Lọc trạng thái: Draft | Signed | Issued | Cancelled (tuỳ chọn)"
                            }
                        },
                        required = new[] { "limit" }
                    }
                }
            }
        }
    };
}
