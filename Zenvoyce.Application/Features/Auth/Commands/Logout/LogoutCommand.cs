using MediatR;

namespace Zenvoyce.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Unit>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    public Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Unit.Value);
    }
}
