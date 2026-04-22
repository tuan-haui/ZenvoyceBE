using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<IReadOnlyCollection<Ttkhachhang>> GetByCompanyAsync(Guid donviId, string? keyword, CancellationToken cancellationToken);
    Task<Ttkhachhang?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken);
    Task<bool> TaxCodeExistsInCompanyAsync(Guid donviId, string masothue, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> HasAnyInvoiceAsync(Guid customerId, CancellationToken cancellationToken);
    Task AddAsync(Ttkhachhang customer, CancellationToken cancellationToken);
    Task UpdateAsync(Ttkhachhang customer, CancellationToken cancellationToken);
}
