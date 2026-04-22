using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Auth.Commands.Login;
using Zenvoyce.Application.Features.Auth.Commands.Logout;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender mediator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await mediator.Send(new LogoutCommand());
        return Ok();
    }
}
