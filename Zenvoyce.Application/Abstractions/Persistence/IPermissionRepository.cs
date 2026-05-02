namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IPermissionRepository
{
    Task AssignMenusAsync(Guid roleId, Guid userId, IReadOnlyCollection<Guid> menuIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetAssignedMenuIdsAsync(Guid roleId, Guid userId, CancellationToken cancellationToken);
}
