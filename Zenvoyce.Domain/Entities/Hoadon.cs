namespace Zenvoyce.Domain.Entities;

public class Hoadon
{
    public Guid Id { get; set; }
    public Guid Donviid { get; set; }
    public Guid Khachhangid { get; set; }
    public Guid Mauctyid { get; set; }
    public string? Kyhieu { get; set; }
    public string? Sohoadon { get; set; }
    public DateTime Ngaylap { get; set; }
    public decimal Tongtien { get; set; }
    public decimal Tienthue { get; set; }
    public decimal Tongthanhtoan { get; set; }
    public string Trangthai { get; set; } = string.Empty;
    /// <summary>Hóa đơn gốc khi lập điều chỉnh/thay thế.</summary>
    public Guid? Thamchieuhoadonid { get; set; }
    public string? Xmldaky { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

public class HoadonHanghoa
{
    public Guid Id { get; set; }
    public Guid Hoadonid { get; set; }
    public Guid Hanghoaid { get; set; }
    public decimal Soluong { get; set; }
    public decimal Dongia { get; set; }
    public decimal Thuesuat { get; set; }
    public decimal Thanhtien { get; set; }
}

public class HoadonLichsu
{
    public Guid Id { get; set; }
    public Guid Hoadonid { get; set; }
    public string Hanhdong { get; set; } = string.Empty;
    public string? Trangthaicu { get; set; }
    public string? Trangthaimoi { get; set; }
    public DateTime Thoigian { get; set; }
    public Guid? Nguoidungid { get; set; }
}
