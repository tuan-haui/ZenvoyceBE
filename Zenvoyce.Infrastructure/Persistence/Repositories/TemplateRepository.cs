using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class TemplateRepository(ZenvoyceDbContext dbContext) : ITemplateRepository
{
    public Task<bool> BaseTemplateCodeExistsAsync(string kyhieu, Guid? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Mauhoadongocs.AnyAsync(
            x => x.Kyhieu == kyhieu && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Mauhoadongoc>> GetBaseTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = await dbContext.Mauhoadongocs
            .AsNoTracking()
            .Where(x => x.IsDeleted != true)
            .OrderBy(x => x.Tenmau)
            .ToListAsync(cancellationToken);

        return templates.Select(MapToDomain).ToArray();
    }

    public async Task<Mauhoadongoc?> GetBaseTemplateByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var template = await dbContext.Mauhoadongocs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return template is null ? null : MapToDomain(template);
    }

    public Task<bool> IsBaseTemplateInUseAsync(Guid baseTemplateId, CancellationToken cancellationToken)
    {
        return dbContext.Mauchocties.AnyAsync(x => x.Maugocid == baseTemplateId, cancellationToken);
    }

    public async Task AddBaseTemplateAsync(Mauhoadongoc template, CancellationToken cancellationToken)
    {
        var entity = new Entities.Mauhoadongoc
        {
            Id = template.Id,
            Tenmau = template.Tenmau,
            Loaihoadon = template.Loaihoadon,
            Kyhieu = template.Kyhieu ?? string.Empty,
            HtmlContent = template.HtmlContent,
            CssContent = template.CssContent,
            Version = template.Version,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            CreatedBy = template.CreatedBy,
            UpdatedBy = template.UpdatedBy,
            IsDeleted = template.IsDeleted
        };

        dbContext.Mauhoadongocs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBaseTemplateAsync(Mauhoadongoc template, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Mauhoadongocs.FirstOrDefaultAsync(x => x.Id == template.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn gốc.");

        entity.Tenmau = template.Tenmau;
        entity.Loaihoadon = template.Loaihoadon;
        entity.Kyhieu = template.Kyhieu ?? string.Empty;
        entity.HtmlContent = template.HtmlContent;
        entity.CssContent = template.CssContent;
        entity.Version = template.Version;
        entity.UpdatedAt = template.UpdatedAt;
        entity.UpdatedBy = template.UpdatedBy;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> BaseTemplateExistsAsync(Guid baseTemplateId, CancellationToken cancellationToken)
    {
        return dbContext.Mauhoadongocs.AnyAsync(x => x.Id == baseTemplateId, cancellationToken);
    }

    public Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return dbContext.Ttcties.AnyAsync(x => x.Id == companyId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<MauchoctyListItem>> GetCompanyTemplatesAsync(
        Guid donviId,
        string? kyhieuMau,
        string? loaiHoadon,
        short? trangthaiPhatHanh,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Mauchocties
            .AsNoTracking()
            .Include(x => x.Maugoc)
            .Include(x => x.Thongtinhdmaus)
            .Where(x => x.Donviid == donviId);

        if (!string.IsNullOrWhiteSpace(kyhieuMau))
        {
            var normalized = kyhieuMau.Trim().ToLower();
            query = query.Where(x => x.Maugoc != null && x.Maugoc.Kyhieu != null && x.Maugoc.Kyhieu.ToLower().Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(loaiHoadon))
        {
            var normalized = loaiHoadon.Trim().ToLower();
            query = query.Where(x => x.Maugoc != null && x.Maugoc.Loaihoadon != null && x.Maugoc.Loaihoadon.ToLower().Contains(normalized));
        }

        if (trangthaiPhatHanh.HasValue)
        {
            query = query.Where(x => x.Trangthaiphathanh == trangthaiPhatHanh.Value);
        }

        var templates = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        return templates.Select(x => new MauchoctyListItem
        {
            Id = x.Id,
            Maugocid = x.Maugocid ?? Guid.Empty,
            Donviid = x.Donviid ?? Guid.Empty,
            Tenmau = x.Maugoc?.Tenmau,
            Loaihoadon = x.Maugoc?.Loaihoadon,
            Kyhieu = x.Maugoc?.Kyhieu,
            Css = x.Css,
            Header = x.Header,
            Trangthaiphathanh = x.Trangthaiphathanh ?? 0,
            Lamaumacdinh = x.Lamaumacdinh ?? false,
            Ngaykichhoat = ToDateTime(x.Ngaykichhoat),
            UpdatedAt = x.UpdatedAt ?? DateTime.MinValue,
            Metadata = x.Thongtinhdmaus.Select(m => new Thongtinhdmau
            {
                Id = m.Id,
                Mauctyid = m.Mauctyid ?? Guid.Empty,
                Tentruong = m.Tentruong,
                Vitrinam = m.Vitrinam,
                Font = m.Font,
                Canle = m.Canle
            }).ToArray()
        }).ToArray();
    }

    public async Task<Mauchocty?> GetCompanyTemplateByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var template = await dbContext.Mauchocties
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (template is null)
        {
            return null;
        }

        return new Mauchocty
        {
            Id = template.Id,
            Maugocid = template.Maugocid ?? Guid.Empty,
            Donviid = template.Donviid ?? Guid.Empty,
            Css = template.Css,
            Header = template.Header,
            Trangthaiphathanh = template.Trangthaiphathanh ?? 0,
            Lamaumacdinh = template.Lamaumacdinh ?? false,
            Ngaykichhoat = ToDateTime(template.Ngaykichhoat),
            CreatedAt = template.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = template.UpdatedAt ?? DateTime.MinValue,
            CreatedBy = template.CreatedBy,
            UpdatedBy = template.UpdatedBy,
            IsDeleted = template.IsDeleted ?? false
        };
    }

    public async Task UpdateCompanyTemplateStatusAsync(
        Guid id,
        short trangthaiPhatHanh,
        DateTime updatedAt,
        Guid? updatedBy,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Mauchocties.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn của công ty.");

        entity.Trangthaiphathanh = trangthaiPhatHanh;
        entity.UpdatedAt = updatedAt;
        entity.UpdatedBy = updatedBy;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyTemplateAsync(
        Mauchocty companyTemplate,
        IReadOnlyCollection<Thongtinhdmau> metadata,
        bool setDefaultTemplate,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (setDefaultTemplate)
            {
                await dbContext.Mauchocties
                    .Where(x => x.Donviid == companyTemplate.Donviid && x.Lamaumacdinh == true)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.Lamaumacdinh, false)
                            .SetProperty(x => x.UpdatedAt, companyTemplate.UpdatedAt)
                            .SetProperty(x => x.UpdatedBy, companyTemplate.UpdatedBy),
                        cancellationToken);
            }

            var companyTemplateEntity = new Entities.Mauchocty
            {
                Id = companyTemplate.Id,
                Maugocid = companyTemplate.Maugocid,
                Donviid = companyTemplate.Donviid,
                Css = companyTemplate.Css,
                Header = companyTemplate.Header,
                Trangthaiphathanh = companyTemplate.Trangthaiphathanh,
                Lamaumacdinh = companyTemplate.Lamaumacdinh,
                Ngaykichhoat = companyTemplate.Ngaykichhoat,
                CreatedAt = companyTemplate.CreatedAt,
                UpdatedAt = companyTemplate.UpdatedAt,
                CreatedBy = companyTemplate.CreatedBy,
                UpdatedBy = companyTemplate.UpdatedBy,
                IsDeleted = companyTemplate.IsDeleted
            };
            dbContext.Mauchocties.Add(companyTemplateEntity);

            if (metadata.Count > 0)
            {
                var metadataEntities = metadata.Select(x => new Entities.Thongtinhdmau
                {
                    Id = x.Id,
                    Mauctyid = x.Mauctyid,
                    Tentruong = x.Tentruong,
                    Vitrinam = x.Vitrinam,
                    Font = x.Font,
                    Canle = x.Canle
                });
                dbContext.Thongtinhdmaus.AddRange(metadataEntities);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Mauhoadongoc MapToDomain(Entities.Mauhoadongoc entity)
    {
        return new Mauhoadongoc
        {
            Id = entity.Id,
            Tenmau = entity.Tenmau,
            Loaihoadon = entity.Loaihoadon,
            Kyhieu = entity.Kyhieu,
            HtmlContent = entity.HtmlContent,
            CssContent = entity.CssContent,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = entity.UpdatedAt ?? DateTime.MinValue,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            IsDeleted = entity.IsDeleted ?? false
        };
    }

    private static DateTime? ToDateTime(object? value)
    {
        return value switch
        {
            DateTime dateTime => dateTime,
            DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime,
            _ => null
        };
    }
}
