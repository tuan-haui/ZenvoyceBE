using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Auth.Commands.Login;
using Zenvoyce.Application.Features.Auth.Commands.Logout;
using Zenvoyce.Application.Features.Auth.Commands.SeedFirstAdmin;
using Zenvoyce.Application.Features.Auth.DTOs;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender mediator) : ControllerBase
{
    /// <returns>Đăng nhập thành công.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), 200)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Chỉ có hiệu lực khi chưa có bất kỳ người dùng nào; tạo admin / Admin@123.
    /// </summary>
    [HttpPost("seed-first-admin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SeedFirstAdminResponseDto), 200)]
    public async Task<ActionResult<SeedFirstAdminResponseDto>> SeedFirstAdmin()
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
