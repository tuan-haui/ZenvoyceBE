using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Roles.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Tenquyen, string? Mota) : IRequest<RoleDto>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Tenquyen).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Mota).MaximumLength(255);
    }
}

public class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await roleRepository.NameExistsAsync(request.Tenquyen, cancellationToken))
        {
            throw new InvalidOperationException("Tên quyền đã tồn tại.");
        }

        var now = dateTimeProvider.UtcNow;
        var role = new Nhomquyen
        {
            Id = Guid.NewGuid(),
            Tenquyen = request.Tenquyen.Trim(),
            Mota = request.Mota?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        await roleRepository.AddAsync(role, cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Tenquyen = role.Tenquyen,
            Mota = role.Mota
        };
    }
}
