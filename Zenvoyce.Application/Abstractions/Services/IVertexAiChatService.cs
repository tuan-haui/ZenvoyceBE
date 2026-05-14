using Zenvoyce.Application.Features.Ai.DTOs;

namespace Zenvoyce.Application.Abstractions.Services;

/// <summary>
/// AI Chat service hỗ trợ lịch sử hội thoại (memory) và function calling (query DB).
/// </summary>
public interface IVertexAiChatService
{
    /// <summary>
    /// Stream chat với memory và function calling.
    /// Trả về từng chunk text ngay khi có qua IAsyncEnumerable.
    /// </summary>
    IAsyncEnumerable<string> ChatStreamAsync(
        ChatSession session,
        string newMessage,
        CancellationToken cancellationToken = default);
}
