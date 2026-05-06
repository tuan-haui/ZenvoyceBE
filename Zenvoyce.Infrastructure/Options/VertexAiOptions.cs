namespace Zenvoyce.Infrastructure.Options;

public sealed class VertexAiOptions
{
    public const string SectionName = "VertexAi";

    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = "us-central1";
    public string Model { get; set; } = string.Empty;
}
