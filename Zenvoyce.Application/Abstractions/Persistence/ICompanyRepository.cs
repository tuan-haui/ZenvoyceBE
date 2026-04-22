using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface ICompanyRepository
{
    Task<IReadOnlyCollection<Ttcty>> GetAllAsync(CancellationToken cancellationToken);
    Task<Ttcty?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> TaxCodeExistsAsync(string masothue, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> HasAnyInvoiceAsync(Guid companyId, CancellationToken cancellationToken);
    Task AddAsync(Ttcty company, CancellationToken cancellationToken);
    Task UpdateAsync(Ttcty company, CancellationToken cancellationToken);
}
