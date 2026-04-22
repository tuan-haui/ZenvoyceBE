namespace Zenvoyce.Domain.Entities;

public class Nguoidung
{
    public Guid Id { get; set; }
    public Guid? Madonvi { get; set; }
    public string Tendangnhap { get; set; } = string.Empty;
    public string Matkhau { get; set; } = string.Empty;
    public string? Dienthoai { get; set; }
    public short Trangthai { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
