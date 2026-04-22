using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Roles.DTOs;

namespace Zenvoyce.Application.Features.Roles.Queries.GetRoles;

public record GetRolesQuery : IRequest<IReadOnlyCollection<RoleDto>>;

public class GetRolesQueryHandler(IRoleRepository roleRepository) : IRequestHandler<GetRolesQuery, IReadOnlyCollection<RoleDto>>
{
    public async Task<IReadOnlyCollection<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetAllAsync(cancellationToken);
        return roles.Select(x => new RoleDto
        {
            Id = x.Id,
            Tenquyen = x.Tenquyen,
            Mota = x.Mota
        }).ToArray();
    }
}
