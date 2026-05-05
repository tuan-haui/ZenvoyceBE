using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Infrastructure.Persistence.Mappers;
using DomainUser = Zenvoyce.Domain.Entities.Nguoidung;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class UserRepository(ZenvoyceDbContext dbContext) : IUserRepository
{
    public async Task<DomainUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Nguoidungs
            .AsNoTracking()
            .Include(x => x.Quyen)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted != true, cancellationToken);

        return user?.ToDomain();
    }

    public async Task<DomainUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await dbContext.Nguoidungs
            .AsNoTracking()
            .Include(x => x.Quyen)
            .FirstOrDefaultAsync(x => x.Tendangnhap == username && x.IsDeleted != true, cancellationToken);

        return user?.ToDomain();
    }

    public Task<bool> UsernameExistsAsync(string username, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Nguoidungs.AnyAsync(
            x => x.Tendangnhap == username && x.IsDeleted != true && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return dbContext.Nguoidungs.AnyAsync(
            x => x.Email != null
                && x.Email.ToLower() == normalized
                && x.IsDeleted != true
                && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<DomainUser>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedPage = pageNumber < 1 ? 1 : pageNumber;
        var normalizedSize = pageSize <= 0 ? 10 : pageSize;

        var users = await dbContext.Nguoidungs
            .AsNoTracking()
            .Include(x => x.Quyen)
            .Where(x => x.IsDeleted != true)
            .OrderBy(x => x.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);

        return users.Select(x => x.ToDomain()).ToArray();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        var result = await dbContext.Nguoidungs.CountAsync(x => x.IsDeleted != true, cancellationToken);
        return result;
    }

    public async Task AddAsync(DomainUser user, CancellationToken cancellationToken)
    {
        var entity = new Entities.Nguoidung();
        entity.ApplyFromDomain(user);

        dbContext.Nguoidungs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DomainUser user, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Nguoidungs.FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        entity.ApplyFromDomain(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
