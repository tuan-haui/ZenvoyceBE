using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Common.Audit;
using Zenvoyce.Application.Features.Auth.Commands.Login;
using Zenvoyce.Application.Features.Auth.Commands.Logout;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Common.Behaviors;

public class AuditCommandBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    IAuditLogRepository auditLogRepository)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string InvoicesCommandsNamespacePrefix = "Zenvoyce.Application.Features.Invoices.Commands";

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        var type = typeof(TRequest);
        if (!type.Name.EndsWith("Command", StringComparison.Ordinal))
            return response;

        if (request is LoginCommand or LogoutCommand)
            return response;

        var ns = type.Namespace ?? "";
        if (ns.StartsWith(InvoicesCommandsNamespacePrefix, StringComparison.Ordinal))
            return response;

        var action = type.FullName ?? type.Name;
        if (action.Length > 255)
            action = action[..255];

        var detail = CommandAuditDetailFormatter.Format(request);
        await auditLogRepository.AddSystemActivityAsync(currentUser.UserId, action, cancellationToken, detail);

        return response;
    }
}
