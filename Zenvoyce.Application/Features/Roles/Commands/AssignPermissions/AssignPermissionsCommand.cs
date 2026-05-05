using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Roles.Commands.AssignPermissions;

public record AssignPermissionsCommand(Guid RoleId, IReadOnlyCollection<Guid> MenuIds) : IRequest<Unit>;

public class AssignPermissionsCommandValidator : AbstractValidator<AssignPermissionsCommand>
{
    public AssignPermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.MenuIds).NotNull();
        RuleForEach(x => x.MenuIds).NotEmpty();
    }
}

public class AssignPermissionsCommandHandler(
    IRoleRepository roleRepository,
    IMenuRepository menuRepository,
    IPermissionRepository permissionRepository,
    ICurrentUserService currentUserService) : IRequestHandler<AssignPermissionsCommand, Unit>
{
    public async Task<Unit> Handle(AssignPermissionsCommand request, CancellationToken cancellationToken)
    {
        if (!await roleRepository.ExistsAsync(request.RoleId, cancellationToken))
        {
            throw new KeyNotFoundException("Không tìm thấy nhóm quyền.");
        }

        foreach (var menuId in request.MenuIds.Distinct())
        {
            if (!await menuRepository.ExistsAsync(menuId, cancellationToken))
            {
                throw new KeyNotFoundException($"Không tìm thấy menu: {menuId}");
            }
        }

        await permissionRepository.AssignMenusToRoleAsync(
            request.RoleId,
            request.MenuIds.Distinct().ToArray(),
            currentUserService.UserId,
            cancellationToken);

        return Unit.Value;
    }
}
