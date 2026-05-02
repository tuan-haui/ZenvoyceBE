using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Menus.Commands.CreateMenu;
using Zenvoyce.Application.Features.Menus.Queries.GetMenusForRole;
using Zenvoyce.Application.Features.Menus.Queries.GetSidebar;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenusController(ISender mediator) : ControllerBase
{
    [HttpGet("for-role/{roleId:guid}")]
    public async Task<IActionResult> GetMenusForRole(Guid roleId)
    {
        var result = await mediator.Send(new GetMenusForRoleQuery(roleId));
        return Ok(result);
    }

    [HttpGet("sidebar")]
    public async Task<IActionResult> GetSidebar()
    {
        var result = await mediator.Send(new GetSidebarQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMenu([FromBody] CreateMenuCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetSidebar), new { id = result.Id }, result);
    }
}
