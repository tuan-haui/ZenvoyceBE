using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class UserPermissionRepository(ZenvoyceDbContext dbContext) : IUserPermissionRepository
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
}
