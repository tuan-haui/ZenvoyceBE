using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;

namespace Zenvoyce.Application.Features.Roles.Queries.GetAssignedMenuIds;

public record GetAssignedMenuIdsQuery(Guid RoleId) : IRequest<IReadOnlyCollection<Guid>>;

public class GetAssignedMenuIdsQueryHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<GetAssignedMenuIdsQuery, IReadOnlyCollection<Guid>>
{
    public async Task<IReadOnlyCollection<Guid>> Handle(
        GetAssignedMenuIdsQuery request,
        CancellationToken cancellationToken)
    {
        var ids = await permissionRepository.GetAssignedMenuIdsAsync(request.RoleId, cancellationToken);
        return ids;
    }
}
