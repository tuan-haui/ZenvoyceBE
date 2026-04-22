using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;

namespace Zenvoyce.Application.Features.Roles.Commands.AssignPermissions;

public record AssignPermissionsCommand(Guid RoleId, Guid UserId, IReadOnlyCollection<Guid> MenuIds) : IRequest<Unit>;

public class AssignPermissionsCommandValidator : AbstractValidator<AssignPermissionsCommand>
{
    public AssignPermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MenuIds).NotNull();
        RuleForEach(x => x.MenuIds).NotEmpty();
    }
}

public class AssignPermissionsCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IMenuRepository menuRepository,
    IPermissionRepository permissionRepository) : IRequestHandler<AssignPermissionsCommand, Unit>
{
    public async Task<Unit> Handle(AssignPermissionsCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng.");
        }

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

        await permissionRepository.AssignMenusAsync(
            request.RoleId,
            request.UserId,
            request.MenuIds.Distinct().ToArray(),
            cancellationToken);

        return Unit.Value;
    }
}
