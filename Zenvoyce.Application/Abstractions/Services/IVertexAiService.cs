using Zenvoyce.Application.Features.Ai.DTOs;

namespace Zenvoyce.Application.Abstractions.Services;

public interface IVertexAiService
{
    /// <summary>
    /// Non-stream: Chờ model generate xong toàn bộ, trả về response đầy đủ.
    /// </summary>
    Task<AiChatResponseDto> ChatAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream: Trả về từng chunk text ngay khi có (real-time).
    /// </summary>
    IAsyncEnumerable<string> ChatStreamAsync(string message, CancellationToken cancellationToken = default);
}
