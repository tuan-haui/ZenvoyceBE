namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IUserPermissionRepository
{
    Task<IReadOnlyCollection<Guid>> GetRoleIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
