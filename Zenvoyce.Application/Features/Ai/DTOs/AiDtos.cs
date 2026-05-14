namespace Zenvoyce.Application.Features.Ai.DTOs;

/// <summary>
/// Response từ non-stream chat endpoint (full response).
/// </summary>
public sealed class AiChatResponseDto
{
    public string Text { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string FinishReason { get; set; } = string.Empty;
    public AiUsageDto? Usage { get; set; }
}

/// <summary>
/// Token usage information từ Vertex AI.
/// </summary>
public sealed class AiUsageDto
{
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}

/// <summary>
/// Request DTO cho stream chat endpoint.
/// </summary>
public sealed class AiChatStreamRequestDto
{
    /// <summary>
    /// Tin nhắn gửi đến AI model.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Stream chunk response (từng phần trong SSE stream).
/// </summary>
public sealed class AiChatStreamChunkDto
{
    /// <summary>
    /// Phần text của response (có thể rỗng nếu là chunk cuối).
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
