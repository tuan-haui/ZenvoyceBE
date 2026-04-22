namespace Zenvoyce.Application.Features.Invoices.DTOs;

public class InvoiceLineRequestDto
{
    public Guid HanghoaId { get; set; }
    public decimal Soluong { get; set; }
    public decimal Dongia { get; set; }
    public decimal ThueSuat { get; set; }
}

public class CreateInvoiceResultDto
{
    public Guid Id { get; set; }
    public string Trangthai { get; set; } = string.Empty;
    public decimal Tongtien { get; set; }
    public decimal Tienthue { get; set; }
    public decimal Tongthanhtoan { get; set; }
}

public class InvoiceListItemDto
{
    public Guid Id { get; set; }
    public Guid DonviId { get; set; }
    public Guid KhachhangId { get; set; }
    public Guid MauctyId { get; set; }
    public string? Kyhieu { get; set; }
    public string? Sohoadon { get; set; }
    public DateTime Ngaylap { get; set; }
    public decimal Tongtien { get; set; }
    public decimal Tienthue { get; set; }
    public decimal Tongthanhtoan { get; set; }
    public string Trangthai { get; set; } = string.Empty;
}

public class InvoiceHistoryItemDto
{
    public Guid Id { get; set; }
    public Guid HoadonId { get; set; }
    public string Hanhdong { get; set; } = string.Empty;
    public string? TrangthaiCu { get; set; }
    public string? TrangthaiMoi { get; set; }
    public DateTime Thoigian { get; set; }
    public Guid? NguoidungId { get; set; }
}
