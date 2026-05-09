using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<IReadOnlyCollection<Danhmuchanghoa>> GetByCompanyAsync(Guid donviId, string? keyword, CancellationToken cancellationToken);
    Task<Danhmuchanghoa?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken);
    Task<bool> NameExistsInCompanyAsync(Guid donviId, string productName, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsUsedInInvoiceAsync(Guid productId, CancellationToken cancellationToken);
    Task AddAsync(Danhmuchanghoa product, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Danhmuchanghoa> products, CancellationToken cancellationToken);
    Task UpdateAsync(Danhmuchanghoa product, CancellationToken cancellationToken);
}
