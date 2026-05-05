using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class MenuRepository(ZenvoyceDbContext dbContext) : IMenuRepository
{
    public async Task<IReadOnlyCollection<Sysmenu>> GetSidebarByRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var menus = await (
                from g in dbContext.Sysgroupmenus.AsNoTracking()
                join m in dbContext.Sysmenus.AsNoTracking() on g.Menuid equals m.Id
                where g.Quyenid == roleId && (m.IsDeleted != true)
                orderby m.Stt ?? 0, m.Tenmenu
                select m)
            .ToListAsync(cancellationToken);

        return menus.Select(ToDomain).ToArray();
    }

    public Task<IReadOnlyCollection<Sysmenu>> GetMenusByRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return GetSidebarByRoleIdAsync(roleId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Sysmenu>> GetAllMenusAsync(CancellationToken cancellationToken)
    {
        var menus = await dbContext.Sysmenus
            .AsNoTracking()
            .Where(x => x.IsDeleted != true)
            .OrderBy(x => x.Stt ?? 0)
            .ThenBy(x => x.Tenmenu)
            .ToListAsync(cancellationToken);

        return menus.Select(ToDomain).ToArray();
    }

    private static Sysmenu ToDomain(Zenvoyce.Infrastructure.Entities.Sysmenu x) => new()
    {
        Id = x.Id,
        Tenmenu = x.Tenmenu,
        Duongdan = x.Duongdan,
        MenuchaId = x.Menuchaid,
        Icon = x.Icon,
        Stt = x.Stt
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
            Icon = menu.Icon,
            Stt = menu.Stt ?? 0
        };

        dbContext.Sysmenus.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
