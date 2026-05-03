using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
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
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    [HttpPost("initialize-system")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<InitializeSystemResponseDto>>> InitializeSystem()
    {
        var result = await mediator.Send(new InitializeSystemCommand());
        return Ok(ApiResponse<InitializeSystemResponseDto>.Ok(result));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object?>>> Logout()
    {
        await mediator.Send(new LogoutCommand());
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
