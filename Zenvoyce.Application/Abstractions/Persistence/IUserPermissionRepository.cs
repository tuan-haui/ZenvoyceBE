namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IUserPermissionRepository
{
    Task<Guid?> GetRoleIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
