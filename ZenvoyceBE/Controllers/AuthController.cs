using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Auth.Commands.Login;
using Zenvoyce.Application.Features.Auth.Commands.Logout;
using Zenvoyce.Application.Features.Auth.Commands.SeedFirstAdmin;

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

    /// <summary>
    /// Chỉ có hiệu lực khi chưa có bất kỳ người dùng nào; tạo admin / Admin@123.
    /// </summary>
    [HttpPost("seed-first-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedFirstAdmin()
    {
        var result = await mediator.Send(new SeedFirstAdminCommand());
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
