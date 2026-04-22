using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await mediator.Send(new GetUsersQuery(pageNumber, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(result);
    }

    [HttpPatch("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordCommand command)
    {
        await mediator.Send(command with { Id = id });
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteUserCommand(id));
        return Ok();
    }
}
