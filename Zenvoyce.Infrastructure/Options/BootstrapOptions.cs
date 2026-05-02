namespace Zenvoyce.Infrastructure.Options;

/// <summary>
/// Cấu hình bootstrap hệ thống (đọc từ appsettings khóa Bootstrap).
/// </summary>
public class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    /// <summary>Tên đăng nhập admin khi khởi tạo lần đầu.</summary>
    public string AdminUsername { get; set; } = "admin";

    /// <summary>Mật khẩu ban đầu (nên đổi ngay sau đăng nhập).</summary>
    public string AdminPassword { get; set; } = "Admin@123";

    public string AdminFullName { get; set; } = "Quản trị viên";

    public string? AdminEmail { get; set; }
}
