using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class UserPermissionRepository(ZenvoyceDbContext dbContext) : IUserPermissionRepository, IPermissionRepository
{
    public Task<Guid?> GetRoleIdByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Nguoidungs
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.Quyenid)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AssignMenusToRoleAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> menuIds,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Sysgroupmenus
            .Where(x => x.Quyenid == roleId)
            .ToListAsync(cancellationToken);

        dbContext.Sysgroupmenus.RemoveRange(existing);

        if (menuIds.Count > 0)
        {
            var now = DateTime.UtcNow;
            var rows = menuIds.Distinct().Select(menuId => new Entities.Sysgroupmenu
            {
                Id = Guid.NewGuid(),
                Quyenid = roleId,
                Menuid = menuId,
                CreatedAt = now,
                CreatedBy = actorUserId
            });

            await dbContext.Sysgroupmenus.AddRangeAsync(rows, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAssignedMenuIdsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return await dbContext.Sysgroupmenus
            .AsNoTracking()
            .Where(x => x.Quyenid == roleId)
            .Select(x => x.Menuid)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
