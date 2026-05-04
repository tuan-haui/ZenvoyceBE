using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.SystemLogs.DTOs;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task AddSystemActivityAsync(Guid? userId, string action, CancellationToken cancellationToken, string? detail = null);

    Task<PagedResult<AuditLogDto>> GetPagedAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? userId,
        string? actionType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
