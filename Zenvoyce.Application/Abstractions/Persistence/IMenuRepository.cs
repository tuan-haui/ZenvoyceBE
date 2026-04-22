using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IMenuRepository
{
    Task<IReadOnlyCollection<Sysmenu>> GetSidebarByRoleIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);
    Task<bool> RouteExistsAsync(string routePath, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid menuId, CancellationToken cancellationToken);
    Task AddAsync(Sysmenu menu, CancellationToken cancellationToken);
}
