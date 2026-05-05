namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IPermissionRepository
{
    Task AssignMenusToRoleAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> menuIds,
        Guid? actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetAssignedMenuIdsAsync(Guid roleId, CancellationToken cancellationToken);
}
