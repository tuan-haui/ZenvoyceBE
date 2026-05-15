namespace Zenvoyce.Application.Features.Users.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public Guid? Madonvi { get; set; }
    public string Tendangnhap { get; set; } = string.Empty;
    public string? Hoten { get; set; }
    public string? Email { get; set; }
    public string? Dienthoai { get; set; }
    public short Trangthai { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? Quyenid { get; set; }
    public string? Tenquyen { get; set; }
}

public class LoginUserInfoDto
{
    public Guid Id { get; set; }
    public Guid? Madonvi { get; set; }
    public string Tendangnhap { get; set; } = string.Empty;
    public string? Hoten { get; set; }
    public string? Email { get; set; }
    public short Trangthai { get; set; }
    public Guid? Quyenid { get; set; }
    public string? Tenquyen { get; set; }
}
