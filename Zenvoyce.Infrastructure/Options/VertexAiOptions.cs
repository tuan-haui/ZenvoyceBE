namespace Zenvoyce.Infrastructure.Options;

public sealed class VertexAiOptions
{
    public const string SectionName = "VertexAi";

    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = "us-central1";
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Số turn tối đa giữ trong lịch sử hội thoại (mỗi turn = 1 cặp user+model).
    /// </summary>
    public int MaxHistoryTurns { get; set; } = 20;
}
