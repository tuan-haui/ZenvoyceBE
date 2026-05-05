using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class ProductRepository(ZenvoyceDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyCollection<Danhmuchanghoa>> GetByCompanyAsync(Guid donviId, string? keyword, CancellationToken cancellationToken)
    {
        var query = dbContext.Danhmuchanghoas
            .AsNoTracking()
            .Where(x => x.Donviid == donviId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLower();
            query = query.Where(x => x.Tenhanghoa.ToLower().Contains(normalizedKeyword));
        }

        var products = await query
            .OrderBy(x => x.Tenhanghoa)
            .ToListAsync(cancellationToken);

        return products.Select(MapToDomain).ToArray();
    }

    public async Task<Danhmuchanghoa?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Danhmuchanghoas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return product is null ? null : MapToDomain(product);
    }

    public Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return dbContext.Ttcties.AnyAsync(x => x.Id == companyId, cancellationToken);
    }

    public Task<bool> NameExistsInCompanyAsync(Guid donviId, string productName, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Danhmuchanghoas.AnyAsync(
            x => x.Donviid == donviId &&
                 x.Tenhanghoa == productName &&
                 (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> IsUsedInInvoiceAsync(Guid productId, CancellationToken cancellationToken)
    {
        return dbContext.Hoadonchitiets.AnyAsync(x => x.Hanghoaid == productId, cancellationToken);
    }

    public async Task AddAsync(Danhmuchanghoa product, CancellationToken cancellationToken)
    {
        var entity = new Entities.Danhmuchanghoa
        {
            Id = product.Id,
            Donviid = product.Donviid,
            Tenhanghoa = product.Tenhanghoa,
            Donvitinh = product.Donvitinh,
            Dongia = product.Dongia,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            CreatedBy = product.CreatedBy,
            UpdatedBy = product.UpdatedBy,
            IsDeleted = product.IsDeleted
        };

        dbContext.Danhmuchanghoas.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Danhmuchanghoa product, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Danhmuchanghoas.FirstOrDefaultAsync(x => x.Id == product.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hàng hóa.");

        entity.Tenhanghoa = product.Tenhanghoa;
        entity.Donvitinh = product.Donvitinh;
        entity.Dongia = product.Dongia;
        entity.UpdatedAt = product.UpdatedAt;
        entity.UpdatedBy = product.UpdatedBy;
        entity.IsDeleted = product.IsDeleted;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Danhmuchanghoa MapToDomain(Entities.Danhmuchanghoa entity)
    {
        return new Danhmuchanghoa
        {
            Id = entity.Id,
            Donviid = entity.Donviid ?? Guid.Empty,
            Tenhanghoa = entity.Tenhanghoa,
            Donvitinh = entity.Donvitinh,
            Dongia = entity.Dongia ?? 0,
            CreatedAt = entity.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = entity.UpdatedAt ?? DateTime.MinValue,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            IsDeleted = entity.IsDeleted ?? false
        };
    }
}
