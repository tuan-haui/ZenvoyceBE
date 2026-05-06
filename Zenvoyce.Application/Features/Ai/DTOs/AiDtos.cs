namespace Zenvoyce.Application.Features.Ai.DTOs;

public sealed class AiChatResponseDto
{
    public string Text { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string FinishReason { get; set; } = string.Empty;
    public AiUsageDto? Usage { get; set; }
}

public sealed class AiUsageDto
{
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}
