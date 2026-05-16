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
    public Guid? ThamChieuHoadonId { get; set; }
    public string? Kyhieu { get; set; }
    public string? Sohoadon { get; set; }
    public DateTime Ngaylap { get; set; }
    public decimal Tongtien { get; set; }
    public decimal Tienthue { get; set; }
    public decimal Tongthanhtoan { get; set; }
    public string Trangthai { get; set; } = string.Empty;
    public string TenKhachhang { get; set; } = string.Empty;
    public string? MaSoThueKhachhang { get; set; }
    public string? EmailKhachhang { get; set; }
    public string TenDonvi { get; set; } = string.Empty;
    public string? TenMau { get; set; }
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

public class InvoicePreviewResultDto
{
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}

public class InvoiceLineForExportDto
{
    public Guid HanghoaId { get; set; }
    public string TenHanghoa { get; set; } = string.Empty;
    public decimal Soluong { get; set; }
    public decimal Dongia { get; set; }
    public decimal Thuesuat { get; set; }
    public decimal Thanhtien { get; set; }
}

public class InvoiceForExportDto
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
    public string TenKhachhang { get; set; } = string.Empty;
    public string? MaSoThueKhachhang { get; set; }
    public string? EmailKhachhang { get; set; }
    public string TenDonvi { get; set; } = string.Empty;
    public string? TenMau { get; set; }
    public IReadOnlyCollection<InvoiceLineForExportDto> LineItems { get; set; } = Array.Empty<InvoiceLineForExportDto>();
}

public class ExportResultDto
{
    public byte[] ExcelBytes { get; set; } = Array.Empty<byte>();
    public string Filename { get; set; } = string.Empty;
}
