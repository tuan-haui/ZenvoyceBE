using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class RoleRepository(ZenvoyceDbContext dbContext) : IRoleRepository
{
    public async Task<IReadOnlyCollection<Nhomquyen>> GetAllAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Nhomquyens
            .AsNoTracking()
            .Where(x => x.IsDeleted != true)
            .OrderBy(x => x.Tenquyen)
            .ToListAsync(cancellationToken);

        return roles.Select(x => new Nhomquyen
        {
            Id = x.Id,
            Tenquyen = x.Tenquyen,
            Mota = x.Mota,
            CreatedAt = x.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = x.UpdatedAt ?? DateTime.MinValue,
            CreatedBy = x.CreatedBy,
            UpdatedBy = x.UpdatedBy,
            IsDeleted = x.IsDeleted ?? false
        }).ToArray();
    }

    public Task<bool> NameExistsAsync(string roleName, CancellationToken cancellationToken)
    {
        var normalizedName = roleName.Trim().ToLower();
        return dbContext.Nhomquyens.AnyAsync(
            x => x.Tenquyen.ToLower() == normalizedName && x.IsDeleted != true,
            cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return dbContext.Nhomquyens.AnyAsync(x => x.Id == roleId && x.IsDeleted != true, cancellationToken);
    }

    public async Task AddAsync(Nhomquyen role, CancellationToken cancellationToken)
    {
        var entity = new Entities.Nhomquyen
        {
            Id = role.Id,
            Tenquyen = role.Tenquyen,
            Mota = role.Mota,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            CreatedBy = role.CreatedBy,
            UpdatedBy = role.UpdatedBy,
            IsDeleted = role.IsDeleted
        };

        dbContext.Nhomquyens.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InvalidOperationException("Tên quyền đã tồn tại.");
        }
    }
}
