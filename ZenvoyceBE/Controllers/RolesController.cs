using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Roles.Commands.AssignPermissions;
using Zenvoyce.Application.Features.Roles.Commands.CreateRole;
using Zenvoyce.Application.Features.Roles.DTOs;
using Zenvoyce.Application.Features.Roles.Queries.GetAssignedMenuIds;
using Zenvoyce.Application.Features.Roles.Queries.GetRoles;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RolesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleDto>>>> GetRoles()
    {
        var result = await mediator.Send(new GetRolesQuery());
        return Ok(ApiResponse<IReadOnlyCollection<RoleDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<RoleDto>.Ok(result));
    }

    [HttpGet("{roleId:guid}/assigned-menu-ids")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<Guid>>>> GetAssignedMenuIds(Guid roleId)
    {
        var result = await mediator.Send(new GetAssignedMenuIdsQuery(roleId));
        return Ok(ApiResponse<IReadOnlyCollection<Guid>>.Ok(result));
    }

    [HttpPut("{id:guid}/assign-permissions")]
    public async Task<ActionResult<ApiResponse<object?>>> AssignPermissions(Guid id,
        [FromBody] AssignPermissionsRequestDto payload)
    {
        if (payload.RoleId != Guid.Empty && payload.RoleId != id)
        {
            return BadRequest(ApiResponse<object?>.Fail("RoleId trong body không khớp với route id."));
        }

        await mediator.Send(new AssignPermissionsCommand(id, payload.MenuIds));
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
