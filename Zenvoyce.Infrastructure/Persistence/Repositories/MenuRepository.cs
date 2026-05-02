using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class MenuRepository(ZenvoyceDbContext dbContext) : IMenuRepository
{
    public async Task<IReadOnlyCollection<Sysmenu>> GetSidebarByRoleIdsAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        var menus = await dbContext.Sysmenus
            .AsNoTracking()
            .Where(x => x.Quyenid.HasValue && roleIds.Contains(x.Quyenid.Value))
            .OrderBy(x => x.Tenmenu)
            .ToListAsync(cancellationToken);

        return menus.Select(x => new Sysmenu
        {
            Id = x.Id,
            Tenmenu = x.Tenmenu,
            Duongdan = x.Duongdan,
            MenuchaId = x.Menuchaid,
            QuyenId = x.Quyenid
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<Sysmenu>> GetMenusByRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var menus = await dbContext.Sysmenus
            .AsNoTracking()
            .Where(x => x.Quyenid == roleId)
            .OrderBy(x => x.Tenmenu)
            .ToListAsync(cancellationToken);

        return menus.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyCollection<Sysmenu>> GetAllMenusAsync(CancellationToken cancellationToken)
    {
        var menus = await dbContext.Sysmenus
            .AsNoTracking()
            .OrderBy(x => x.Tenmenu)
            .ToListAsync(cancellationToken);

        return menus.Select(ToDomain).ToArray();
    }

    private static Sysmenu ToDomain(Zenvoyce.Infrastructure.Entities.Sysmenu x) => new()
    {
        Id = x.Id,
        Tenmenu = x.Tenmenu,
        Duongdan = x.Duongdan,
        MenuchaId = x.Menuchaid,
        QuyenId = x.Quyenid
    };

    public Task<bool> RouteExistsAsync(string routePath, CancellationToken cancellationToken)
    {
        return dbContext.Sysmenus.AnyAsync(
            x => x.Duongdan != null && x.Duongdan.ToLower() == routePath.ToLower(),
            cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid menuId, CancellationToken cancellationToken)
    {
        return dbContext.Sysmenus.AnyAsync(x => x.Id == menuId, cancellationToken);
    }

    public async Task AddAsync(Sysmenu menu, CancellationToken cancellationToken)
    {
        var entity = new Entities.Sysmenu
        {
            Id = menu.Id,
            Tenmenu = menu.Tenmenu,
            Duongdan = menu.Duongdan,
            Menuchaid = menu.MenuchaId,
            Quyenid = menu.QuyenId
        };

        dbContext.Sysmenus.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
