using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<Nguoidung?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Nguoidung?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> UsernameExistsAsync(string username, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Nguoidung>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task AddAsync(Nguoidung user, CancellationToken cancellationToken);
    Task UpdateAsync(Nguoidung user, CancellationToken cancellationToken);
}
