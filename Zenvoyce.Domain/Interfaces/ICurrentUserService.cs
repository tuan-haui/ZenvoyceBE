namespace Zenvoyce.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>Nhóm quyền hiện tại (claim role_id).</summary>
    Guid? RoleId { get; }

    /// <summary>Tên nhóm quyền (claim role_name).</summary>
    string? RoleName { get; }

    /// <summary>Đơn vị/công ty gắn với user (claim company_id).</summary>
    Guid? CompanyId { get; }

    /// <summary>Tên đăng nhập (claim username).</summary>
    string? Username { get; }

    /// <summary>Họ tên (claim name).</summary>
    string? FullName { get; }

    /// <summary>Email (claim email).</summary>
    string? Email { get; }
}
