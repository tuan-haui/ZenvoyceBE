using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IMenuRepository
{
    Task<IReadOnlyCollection<Sysmenu>> GetSidebarByRoleIdAsync(Guid roleId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Sysmenu>> GetMenusByRoleIdAsync(Guid roleId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Sysmenu>> GetAllMenusAsync(CancellationToken cancellationToken);
    Task<bool> RouteExistsAsync(string routePath, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid menuId, CancellationToken cancellationToken);
    Task AddAsync(Sysmenu menu, CancellationToken cancellationToken);
}
