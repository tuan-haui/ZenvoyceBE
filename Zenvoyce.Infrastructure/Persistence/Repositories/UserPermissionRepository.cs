using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class UserPermissionRepository(ZenvoyceDbContext dbContext) : IUserPermissionRepository, IPermissionRepository
{
    public async Task<IReadOnlyCollection<Guid>> GetRoleIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = await dbContext.Phanquyenchucnangs
            .AsNoTracking()
            .Where(x => x.Nguoidungid == userId)
            .Select(x => x.Quyenid)
            .Distinct()
            .ToListAsync(cancellationToken);

        return roleIds;
    }

    public async Task AssignMenusAsync(
        Guid roleId,
        Guid userId,
        IReadOnlyCollection<Guid> menuIds,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingPermissions = await dbContext.Phanquyenchucnangs
            .Where(x => x.Nguoidungid == userId && x.Quyenid == roleId)
            .ToListAsync(cancellationToken);

        dbContext.Phanquyenchucnangs.RemoveRange(existingPermissions);

        if (menuIds.Count > 0)
        {
            var newPermissions = menuIds.Select(menuId => new Entities.Phanquyenchucnang
            {
                Nguoidungid = userId,
                Quyenid = roleId,
                Menuid = menuId
            });

            await dbContext.Phanquyenchucnangs.AddRangeAsync(newPermissions, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAssignedMenuIdsAsync(
        Guid roleId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Phanquyenchucnangs
            .AsNoTracking()
            .Where(x => x.Nguoidungid == userId && x.Quyenid == roleId)
            .Select(x => x.Menuid)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
