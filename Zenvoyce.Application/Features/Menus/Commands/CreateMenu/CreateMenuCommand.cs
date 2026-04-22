using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Menus.DTOs;
using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Features.Menus.Commands.CreateMenu;

public record CreateMenuCommand(
    string Tenmenu,
    string? Duongdan,
    Guid? MenuchaId,
    Guid? QuyenId) : IRequest<MenuDto>;

public class CreateMenuCommandValidator : AbstractValidator<CreateMenuCommand>
{
    public CreateMenuCommandValidator()
    {
        RuleFor(x => x.Tenmenu).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Duongdan).MaximumLength(255);
    }
}

public class CreateMenuCommandHandler(
    IMenuRepository menuRepository,
    IRoleRepository roleRepository) : IRequestHandler<CreateMenuCommand, MenuDto>
{
    public async Task<MenuDto> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Duongdan)
            && await menuRepository.RouteExistsAsync(request.Duongdan.Trim(), cancellationToken))
        {
            throw new InvalidOperationException("Đường dẫn menu đã tồn tại.");
        }

        if (request.QuyenId.HasValue && !await roleRepository.ExistsAsync(request.QuyenId.Value, cancellationToken))
        {
            throw new KeyNotFoundException("Không tìm thấy nhóm quyền.");
        }

        var menu = new Sysmenu
        {
            Id = Guid.NewGuid(),
            Tenmenu = request.Tenmenu.Trim(),
            Duongdan = request.Duongdan?.Trim(),
            MenuchaId = request.MenuchaId,
            QuyenId = request.QuyenId
        };

        await menuRepository.AddAsync(menu, cancellationToken);

        return new MenuDto
        {
            Id = menu.Id,
            Tenmenu = menu.Tenmenu,
            Duongdan = menu.Duongdan,
            MenuchaId = menu.MenuchaId,
            QuyenId = menu.QuyenId
        };
    }
}
