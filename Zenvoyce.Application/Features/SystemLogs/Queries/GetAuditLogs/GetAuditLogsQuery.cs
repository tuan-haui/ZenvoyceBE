using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.SystemLogs.DTOs;

namespace Zenvoyce.Application.Features.SystemLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? UserId,
    string? ActionType,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<AuditLogDto>>;

public class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("Khoảng thời gian không hợp lệ.");
    }
}

public class GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        return auditLogRepository.GetPagedAsync(
            request.FromDate,
            request.ToDate,
            request.UserId,
            request.ActionType,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
