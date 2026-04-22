using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Roles.Commands.AssignPermissions;
using Zenvoyce.Application.Features.Roles.Commands.CreateRole;
using Zenvoyce.Application.Features.Roles.DTOs;
using Zenvoyce.Application.Features.Roles.Queries.GetRoles;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var result = await mediator.Send(new GetRolesQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetRoles), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/assign-permissions")]
    public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionsRequestDto payload)
    {
        if (payload.RoleId != Guid.Empty && payload.RoleId != id)
        {
            return BadRequest(new { message = "RoleId trong body không khớp với route id." });
        }

        await mediator.Send(new AssignPermissionsCommand(id, payload.UserId, payload.MenuIds));
        return Ok();
    }
}
