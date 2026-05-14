using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Ai.Commands.ChatWithVertexAi;
using Zenvoyce.Application.Features.Ai.DTOs;
using Zenvoyce.Infrastructure.Services.Ai;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AiController(
    ISender mediator,
    IVertexAiService vertexAiService,
    IVertexAiChatService vertexAiChatService,
    ChatSessionStore sessionStore) : ControllerBase
{
    // ─── Endpoint cũ (giữ nguyên) ────────────────────────────────────────────

    /// <summary>
    /// Non-stream: Chờ model generate xong, trả về response đầy đủ.
    /// </summary>
    [HttpPost("chat")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AiChatResponseDto>>> Chat([FromBody] ChatWithVertexAiCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<AiChatResponseDto>.Ok(result));
    }

    /// <summary>
    /// Stream đơn giản (không có memory, không có function calling).
    /// </summary>
    [HttpGet("chat-stream")]
    [AllowAnonymous]
    public async Task StreamChat(
        [FromQuery(Name = "message")] string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Message is required.", cancellationToken);
            return;
        }

        Response.Headers["Content-Type"]      = "text/event-stream";
        Response.Headers["Cache-Control"]     = "no-cache";
        Response.Headers["Connection"]        = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var chunk in vertexAiService.ChatStreamAsync(message, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var chunkDto  = new { text = chunk };
                var jsonChunk = System.Text.Json.JsonSerializer.Serialize(
                    chunkDto,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });

                var data = $"data: {jsonChunk}\n\n";
                await Response.WriteAsync(data, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) { /* Client đóng connection */ }
        catch (Exception ex)
        {
            var errorData = $"data: {{\"error\": \"{System.Text.Json.JsonSerializer.Serialize(ex.Message)}\"}}\n\n";
            await Response.WriteAsync(errorData, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    // ─── Endpoint mới: Memory + Function Calling ──────────────────────────────

    /// <summary>
    /// Stream chat với memory (lịch sử hội thoại) và function calling (query DB).
    /// Client gửi sessionId để duy trì ngữ cảnh hội thoại giữa các request.
    ///
    /// Format SSE:
    ///   data: "chunk text"\n\n  (mỗi chunk)
    ///   data: [DONE]\n\n        (kết thúc)
    ///
    /// Ví dụ:
    ///   GET /api/ai/stream?sessionId=user-123&amp;message=Tổng hóa đơn tháng 5 năm 2026?
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamWithMemory(
        [FromQuery] string sessionId,
        [FromQuery] string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("sessionId is required.", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("message is required.", cancellationToken);
            return;
        }

        // [QUY TẮC 6, 7] Headers SSE
        Response.Headers["Content-Type"]      = "text/event-stream";
        Response.Headers["Cache-Control"]     = "no-cache";
        Response.Headers["Connection"]        = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // Tắt Nginx buffer

        var session = sessionStore.GetOrCreate(sessionId);

        try
        {
            await foreach (var chunk in vertexAiChatService.ChatStreamAsync(session, message, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var data = $"data: {System.Text.Json.JsonSerializer.Serialize(chunk)}\n\n";
                await Response.WriteAsync(data, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken); // [QUY TẮC 6]
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) { /* Client đóng connection */ }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var errorData = $"data: {{\"error\": \"{System.Text.Json.JsonSerializer.Serialize(ex.Message)}\"}}\n\n";
                await Response.WriteAsync(errorData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Xoá lịch sử hội thoại của một session.
    /// Gọi khi user bắt đầu cuộc hội thoại mới.
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    public IActionResult ClearSession(string sessionId)
    {
        sessionStore.Clear(sessionId);
        return NoContent();
    }

    /// <summary>
    /// Lấy danh sách các session đang hoạt động (dùng cho debug/admin).
    /// </summary>
    [HttpGet("sessions")]
    public IActionResult GetActiveSessions()
    {
        var sessions = sessionStore.GetActiveSessions();
        return Ok(ApiResponse<IEnumerable<string>>.Ok(sessions));
    }
}
