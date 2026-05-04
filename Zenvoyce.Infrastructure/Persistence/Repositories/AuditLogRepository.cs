using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.SystemLogs.DTOs;
using Zenvoyce.Infrastructure.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class AuditLogRepository(ZenvoyceDbContext dbContext) : IAuditLogRepository
{
    public async Task AddSystemActivityAsync(Guid? userId, string action, CancellationToken cancellationToken, string? detail = null)
    {
        var text = action.Length <= 255 ? action : action[..255];
        SplitDetail(detail, out var part1, out var part2);
        dbContext.Lichsuhoadons.Add(new Lichsuhoadon
        {
            Id = Guid.NewGuid(),
            Hoadonid = null,
            Nguoidungid = userId,
            Hanhdong = text,
            Thoigian = DateTime.UtcNow,
            Trangthaicu = part1,
            Trangthaimoi = part2
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? CombineDetailSegments(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            return null;
        if (string.IsNullOrEmpty(b))
            return a;
        if (string.IsNullOrEmpty(a))
            return b;
        return a + b;
    }

    private static void SplitDetail(string? detail, out string? part1, out string? part2)
    {
        if (string.IsNullOrEmpty(detail))
        {
            part1 = null;
            part2 = null;
            return;
        }

        part1 = detail.Length <= 100 ? detail : detail[..100];
        part2 = detail.Length <= 100 ? null : (detail.Length <= 200 ? detail[100..] : detail[100..200]);
    }

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

        var rows = await query
            .OrderByDescending(x => x.Thoigian)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(x => new
            {
                x.Id,
                x.Nguoidungid,
                Username = x.Nguoidung != null ? x.Nguoidung.Tendangnhap : null,
                x.Hoadonid,
                x.Hanhdong,
                x.Thoigian,
                x.Trangthaicu,
                x.Trangthaimoi
            })
            .ToListAsync(cancellationToken);

        var data = rows.Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.Nguoidungid,
                Username = x.Username,
                InvoiceId = x.Hoadonid,
                ActionType = x.Hanhdong,
                ActionTime = x.Thoigian,
                Detail = CombineDetailSegments(x.Trangthaicu, x.Trangthaimoi)
            })
            .ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = data,
            PageNumber = normalizedPage,
            PageSize = normalizedSize,
            TotalCount = totalCount
        };
    }
}
