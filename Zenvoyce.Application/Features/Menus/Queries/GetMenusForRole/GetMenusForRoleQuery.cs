using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Menus.DTOs;

namespace Zenvoyce.Application.Features.Menus.Queries.GetMenusForRole;

public record GetMenusForRoleQuery(Guid RoleId) : IRequest<IReadOnlyCollection<MenuDto>>;

public class GetMenusForRoleQueryHandler(IMenuRepository menuRepository)
    : IRequestHandler<GetMenusForRoleQuery, IReadOnlyCollection<MenuDto>>
{
    public async Task<IReadOnlyCollection<MenuDto>> Handle(
        GetMenusForRoleQuery request,
        CancellationToken cancellationToken)
    {
        var menus = await menuRepository.GetMenusByRoleIdAsync(request.RoleId, cancellationToken);
        if (menus.Count == 0)
        {
            menus = await menuRepository.GetAllMenusAsync(cancellationToken);
        }

        return menus
            .Select(x => new MenuDto
            {
                Id = x.Id,
                Tenmenu = x.Tenmenu,
                Duongdan = x.Duongdan,
                MenuchaId = x.MenuchaId,
                QuyenId = x.QuyenId
            })
            .ToArray();
    }
}
