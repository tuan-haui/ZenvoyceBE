using System.Text.Json;

namespace Zenvoyce.Application.Features.Ai.DTOs;

/// <summary>
/// Một lượt trong lịch sử hội thoại AI (user / model / tool).
/// Đặt trong Application layer để IVertexAiChatService có thể dùng.
/// </summary>
public sealed class ChatTurn
{
    public string   Role  { get; init; } = string.Empty;
    public object[] Parts { get; init; } = Array.Empty<object>();
}

/// <summary>
/// Lưu toàn bộ lịch sử hội thoại của một session.
/// Vertex AI không tự lưu trạng thái — mỗi request phải gửi toàn bộ history.
/// </summary>
public sealed class ChatSession
{
    private readonly int _maxTurns;

    public ChatSession(int maxTurns = 20) => _maxTurns = maxTurns;

    public List<ChatTurn> History { get; } = new();

    /// <summary>User gửi text message.</summary>
    public void AddUser(string text) => AddAndTrim(new ChatTurn
    {
        Role  = "user",
        Parts = new object[] { new { text } }
    });

    /// <summary>Model trả về text.</summary>
    public void AddModel(string text) => History.Add(new ChatTurn
    {
        Role  = "model",
        Parts = new object[] { new { text } }
    });

    /// <summary>Model yêu cầu gọi tool (role = "model").</summary>
    public void AddFunctionCall(string name, JsonElement args) => History.Add(new ChatTurn
    {
        Role  = "model",
        Parts = new object[]
        {
            new { functionCall = new { name, args } }
        }
    });

    /// <summary>Kết quả tool trả về (role = "tool").</summary>
    public void AddFunctionResponse(string name, object result) => History.Add(new ChatTurn
    {
        Role  = "tool",
        Parts = new object[]
        {
            new
            {
                functionResponse = new
                {
                    name,
                    response = new { content = result }
                }
            }
        }
    });

    /// <summary>Xoá history cũ, giữ lại N turn gần nhất.</summary>
    private void AddAndTrim(ChatTurn turn)
    {
        History.Add(turn);
        while (History.Count > _maxTurns * 2)
            History.RemoveRange(0, 2);
    }
}
