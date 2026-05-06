using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Ai.Commands.ChatWithVertexAi;
using Zenvoyce.Application.Features.Ai.DTOs;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AiController(ISender mediator) : ControllerBase
{
    [HttpPost("chat")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AiChatResponseDto>>> Chat([FromBody] ChatWithVertexAiCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<AiChatResponseDto>.Ok(result));
    }
}
