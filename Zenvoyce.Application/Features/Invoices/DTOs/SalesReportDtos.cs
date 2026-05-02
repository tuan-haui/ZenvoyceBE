namespace Zenvoyce.Application.Features.Invoices.DTOs;

public class SalesReportRow
{
    public Guid KhachhangId { get; set; }
    public string TenKhachHang { get; set; } = string.Empty;
    public int SoHoaDon { get; set; }
    public decimal TongTienHang { get; set; }
    public decimal TienThue { get; set; }
    public decimal TongThanhToan { get; set; }
}
