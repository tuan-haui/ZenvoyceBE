using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Auth.Commands.Login;
using Zenvoyce.Application.Features.Auth.Commands.Logout;
using Zenvoyce.Application.Features.System.Commands.InitializeSystem;
using Zenvoyce.Application.Features.System.DTOs;
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
    /// Khởi tạo hệ thống (một lần): nhóm quyền + menu + phân quyền sidebar + admin từ appsettings (Bootstrap).
    /// Chỉ chạy khi chưa có người dùng nào.
    /// </summary>
    [HttpPost("initialize-system")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InitializeSystemResponseDto), 200)]
    public async Task<ActionResult<InitializeSystemResponseDto>> InitializeSystem()
    {
        var result = await mediator.Send(new InitializeSystemCommand());
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
