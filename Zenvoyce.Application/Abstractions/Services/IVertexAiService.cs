using Zenvoyce.Application.Features.Ai.DTOs;

namespace Zenvoyce.Application.Abstractions.Services;

public interface IVertexAiService
{
    Task<AiChatResponseDto> ChatAsync(string message, CancellationToken cancellationToken);
}
