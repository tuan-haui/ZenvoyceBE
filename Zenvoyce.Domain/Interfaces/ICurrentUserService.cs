namespace Zenvoyce.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>Nhóm quyền hiện tại (một claim role_id; token cũ có nhiều claim thì lấy claim đầu).</summary>
    Guid? RoleId { get; }
}
