using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Users.DTOs;
using Zenvoyce.Application.Features.Users.Commands.ChangePassword;
using Zenvoyce.Application.Features.Users.Commands.CreateUser;
using Zenvoyce.Application.Features.Users.Commands.DeleteUser;
using Zenvoyce.Application.Features.Users.Commands.UpdateUser;
using Zenvoyce.Application.Features.Users.Queries.GetUserById;
using Zenvoyce.Application.Features.Users.Queries.GetUsers;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await mediator.Send(new GetUsersQuery(pageNumber, pageSize));
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id));
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPatch("{id:guid}/change-password")]
    public async Task<ActionResult<ApiResponse<object?>>> ChangePassword(Guid id, [FromBody] ChangePasswordCommand command)
    {
        await mediator.Send(command with { Id = id });
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id)
    {
        await mediator.Send(new DeleteUserCommand(id));
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
