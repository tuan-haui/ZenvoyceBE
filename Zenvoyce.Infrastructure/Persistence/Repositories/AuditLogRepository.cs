using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.SystemLogs.DTOs;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class AuditLogRepository(ZenvoyceDbContext dbContext) : IAuditLogRepository
{
    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? userId,
        string? actionType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = pageNumber < 1 ? 1 : pageNumber;
        var normalizedSize = pageSize <= 0 ? 20 : pageSize;

        var query = dbContext.Lichsuhoadons
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.Thoigian >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.Thoigian <= toDate.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.Nguoidungid == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(x => x.Hanhdong != null && x.Hanhdong.ToLower().Contains(actionType.ToLower()));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(x => x.Thoigian)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.Nguoidungid,
                Username = x.Nguoidung != null ? x.Nguoidung.Tendangnhap : null,
                InvoiceId = x.Hoadonid,
                ActionType = x.Hanhdong,
                ActionTime = x.Thoigian
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = data,
            PageNumber = normalizedPage,
            PageSize = normalizedSize,
            TotalCount = totalCount
        };
    }
}
