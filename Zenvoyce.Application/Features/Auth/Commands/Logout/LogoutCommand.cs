using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Unit>;

public class LogoutCommandHandler(
    ICurrentUserService currentUser,
    IAuditLogRepository auditLogRepository) : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await auditLogRepository.AddSystemActivityAsync(currentUser.UserId, "Đăng xuất", cancellationToken);
        return Unit.Value;
    }
}
