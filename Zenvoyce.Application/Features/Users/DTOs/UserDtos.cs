namespace Zenvoyce.Application.Features.Users.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public Guid? Madonvi { get; set; }
    public string Tendangnhap { get; set; } = string.Empty;
    public string? Dienthoai { get; set; }
    public short Trangthai { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LoginUserInfoDto
{
    public Guid Id { get; set; }
    public string Tendangnhap { get; set; } = string.Empty;
    public short Trangthai { get; set; }
}
