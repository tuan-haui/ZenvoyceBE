using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Ai.Commands.ChatWithVertexAi;
using Zenvoyce.Application.Features.Ai.DTOs;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AiController(ISender mediator, IVertexAiService vertexAiService) : ControllerBase
{
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
    /// Stream: Trả về từng chunk text ngay khi có (Server-Sent Events).
    /// Client subscribe bằng EventSource hoặc fetch event stream.
    /// </summary>
    /// <remarks>
    /// Endpoint streaming phục vụ cho frontend:
    /// - Format: `data: {"text": "chunk of text"}`
    /// - Kết thúc bằng: `data: [DONE]`
    /// 
    /// Frontend JavaScript example:
    /// ```javascript
    /// const eventSource = new EventSource('/api/ai/chat-stream?message=Hello');
    /// eventSource.addEventListener('message', (event) => {
    ///   const chunk = JSON.parse(event.data);
    ///   console.log(chunk.text);
    /// });
    /// eventSource.addEventListener('done', () => eventSource.close());
    /// ```
    /// </remarks>
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

        // [QUY TẮC SSE 1] Set headers cho Server-Sent Events
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // Tắt Nginx buffer

        try
        {
            // Stream từng chunk từ service
            await foreach (var chunk in vertexAiService.ChatStreamAsync(message, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Format SSE chuẩn: data: {json}\n\n
                var chunkDto = new { text = chunk };
                var jsonChunk = System.Text.Json.JsonSerializer.Serialize(
                    chunkDto,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });

                var data = $"data: {jsonChunk}\n\n";
                await Response.WriteAsync(data, cancellationToken);

                // [QUY TẮC SSE 2] Flush ngay để client nhận được chunk
                await Response.Body.FlushAsync(cancellationToken);
            }

            // Gửi signal kết thúc
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client đóng connection
        }
        catch (Exception ex)
        {
            var errorData = $"data: {{\"error\": \"{System.Text.Json.JsonSerializer.Serialize(ex.Message)}\"}}\n\n";
            await Response.WriteAsync(errorData, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
