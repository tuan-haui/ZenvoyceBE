namespace Zenvoyce.Domain.Entities;

public class Nguoidung
{
    public Guid Id { get; set; }
    public Guid? Madonvi { get; set; }
    public string Tendangnhap { get; set; } = string.Empty;
    public string Matkhau { get; set; } = string.Empty;
    public string? Hoten { get; set; }
    public string? Email { get; set; }
    public string? Dienthoai { get; set; }
    public short Trangthai { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Khóa ngoại tới nhóm quyền (1 user — 1 nhóm quyền).</summary>
    public Guid? Quyenid { get; set; }

    /// <summary>Chỉ dùng khi đọc (join); không map vào bảng khi ghi.</summary>
    public string? Tenquyen { get; set; }
}
