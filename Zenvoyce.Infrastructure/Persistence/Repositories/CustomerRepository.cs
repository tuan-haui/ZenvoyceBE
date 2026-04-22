using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class CustomerRepository(ZenvoyceDbContext dbContext) : ICustomerRepository
{
    public async Task<IReadOnlyCollection<Ttkhachhang>> GetByCompanyAsync(Guid donviId, string? keyword, CancellationToken cancellationToken)
    {
        var query = dbContext.Ttkhachhangs
            .AsNoTracking()
            .Where(x => x.Donviid == donviId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLower();
            query = query.Where(x =>
                x.Tenkhachhang.ToLower().Contains(normalizedKeyword) ||
                (x.Masothue != null && x.Masothue.ToLower().Contains(normalizedKeyword)));
        }

        var customers = await query
            .OrderBy(x => x.Tenkhachhang)
            .ToListAsync(cancellationToken);

        return customers.Select(MapToDomain).ToArray();
    }

    public async Task<Ttkhachhang?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Ttkhachhangs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return customer is null ? null : MapToDomain(customer);
    }

    public Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return dbContext.Ttcties.AnyAsync(x => x.Id == companyId, cancellationToken);
    }

    public Task<bool> TaxCodeExistsInCompanyAsync(Guid donviId, string masothue, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Ttkhachhangs.AnyAsync(
            x => x.Donviid == donviId &&
                 x.Masothue == masothue &&
                 (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> HasAnyInvoiceAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return dbContext.Tthoadons.AnyAsync(x => x.Khachhangid == customerId && x.IsDeleted != true, cancellationToken);
    }

    public async Task AddAsync(Ttkhachhang customer, CancellationToken cancellationToken)
    {
        var entity = new Entities.Ttkhachhang
        {
            Id = customer.Id,
            Donviid = customer.Donviid,
            Tenkhachhang = customer.Tenkhachhang,
            Masothue = customer.Masothue,
            Email = customer.Email,
            Dienthoai = customer.Dienthoai,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            CreatedBy = customer.CreatedBy,
            UpdatedBy = customer.UpdatedBy,
            IsDeleted = customer.IsDeleted
        };

        dbContext.Ttkhachhangs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ttkhachhang customer, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Ttkhachhangs.FirstOrDefaultAsync(x => x.Id == customer.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy khách hàng.");

        entity.Tenkhachhang = customer.Tenkhachhang;
        entity.Masothue = customer.Masothue;
        entity.Email = customer.Email;
        entity.Dienthoai = customer.Dienthoai;
        entity.UpdatedAt = customer.UpdatedAt;
        entity.UpdatedBy = customer.UpdatedBy;
        entity.IsDeleted = customer.IsDeleted;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Ttkhachhang MapToDomain(Entities.Ttkhachhang entity)
    {
        return new Ttkhachhang
        {
            Id = entity.Id,
            Donviid = entity.Donviid ?? Guid.Empty,
            Tenkhachhang = entity.Tenkhachhang,
            Masothue = entity.Masothue,
            Email = entity.Email,
            Dienthoai = entity.Dienthoai,
            CreatedAt = entity.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = entity.UpdatedAt ?? DateTime.MinValue,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            IsDeleted = entity.IsDeleted ?? false
        };
    }
}
