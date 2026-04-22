using AutoMapper;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Users.DTOs;

namespace Zenvoyce.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<UserDto>>;

public class GetUsersQueryHandler(
    IUserRepository userRepository,
    IMapper mapper) : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await userRepository.CountAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = mapper.Map<IReadOnlyCollection<UserDto>>(users),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
