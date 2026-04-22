using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Menus.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Menus.Queries.GetSidebar;

public record GetSidebarQuery : IRequest<IReadOnlyCollection<MenuDto>>;

public class GetSidebarQueryValidator : AbstractValidator<GetSidebarQuery>
{
}

public class GetSidebarQueryHandler(
    ICurrentUserService currentUserService,
    IMenuRepository menuRepository) : IRequestHandler<GetSidebarQuery, IReadOnlyCollection<MenuDto>>
{
    public async Task<IReadOnlyCollection<MenuDto>> Handle(GetSidebarQuery request, CancellationToken cancellationToken)
    {
        var roleIds = currentUserService.RoleIds;
        if (roleIds.Count == 0)
        {
            return [];
        }

        var menus = await menuRepository.GetSidebarByRoleIdsAsync(roleIds, cancellationToken);
        return menus.Select(x => new MenuDto
        {
            Id = x.Id,
            Tenmenu = x.Tenmenu,
            Duongdan = x.Duongdan,
            MenuchaId = x.MenuchaId,
            QuyenId = x.QuyenId
        }).ToArray();
    }
}
