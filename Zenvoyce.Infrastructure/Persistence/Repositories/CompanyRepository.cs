using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class CompanyRepository(ZenvoyceDbContext dbContext) : ICompanyRepository
{
    public async Task<IReadOnlyCollection<Ttcty>> GetAllAsync(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Ttcties
            .AsNoTracking()
            .OrderBy(x => x.Tendonvi)
            .ToListAsync(cancellationToken);

        return companies.Select(MapToDomain).ToArray();
    }

    public async Task<Ttcty?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await dbContext.Ttcties
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return company is null ? null : MapToDomain(company);
    }

    public Task<bool> TaxCodeExistsAsync(string masothue, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Ttcties.AnyAsync(
            x => x.Masothue == masothue && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> HasAnyInvoiceAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return dbContext.Tthoadons.AnyAsync(x => x.Donviid == companyId && x.IsDeleted != true, cancellationToken);
    }

    public async Task AddAsync(Ttcty company, CancellationToken cancellationToken)
    {
        var entity = new Entities.Ttcty
        {
            Id = company.Id,
            Masothue = company.Masothue,
            Tendonvi = company.Tendonvi,
            Diachi = company.Diachi,
            Dienthoai = company.Dienthoai,
            Nguoidaidien = company.Nguoidaidien,
            Emailcongty = company.Emailcongty,
            BankId = company.BankId,
            BankAccount = company.BankAccount,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            CreatedBy = company.CreatedBy,
            UpdatedBy = company.UpdatedBy,
            IsDeleted = company.IsDeleted,
            Trangthai = company.Trangthai
        };

        dbContext.Ttcties.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ttcty company, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Ttcties.FirstOrDefaultAsync(x => x.Id == company.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy công ty.");

        entity.Masothue = company.Masothue;
        entity.Tendonvi = company.Tendonvi;
        entity.Diachi = company.Diachi;
        entity.Dienthoai = company.Dienthoai;
        entity.Nguoidaidien = company.Nguoidaidien;
        entity.Emailcongty = company.Emailcongty;
        entity.BankId = company.BankId;
        entity.BankAccount = company.BankAccount;
        entity.Trangthai = company.Trangthai;
        entity.UpdatedAt = company.UpdatedAt;
        entity.UpdatedBy = company.UpdatedBy;
        entity.IsDeleted = company.IsDeleted;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Ttcty MapToDomain(Entities.Ttcty entity)
    {
        return new Ttcty
        {
            Id = entity.Id,
            Masothue = entity.Masothue,
            Tendonvi = entity.Tendonvi,
            Diachi = entity.Diachi,
            Dienthoai = entity.Dienthoai,
            Nguoidaidien = entity.Nguoidaidien,
            Emailcongty = entity.Emailcongty,
            BankId = entity.BankId,
            BankAccount = entity.BankAccount,
            Trangthai = entity.Trangthai ?? 1,
            CreatedAt = entity.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = entity.UpdatedAt ?? DateTime.MinValue,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            IsDeleted = entity.IsDeleted ?? false
        };
    }
}
