using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<Nhomquyen>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string roleName, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid roleId, CancellationToken cancellationToken);
    Task AddAsync(Nhomquyen role, CancellationToken cancellationToken);
}
