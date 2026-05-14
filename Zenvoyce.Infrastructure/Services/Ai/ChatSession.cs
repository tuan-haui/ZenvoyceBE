using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Zenvoyce.Application.Features.Ai.DTOs;
using Zenvoyce.Infrastructure.Options;

namespace Zenvoyce.Infrastructure.Services.Ai;

/// <summary>
/// Quản lý nhiều session đồng thời (in-memory).
/// Đăng ký là Singleton.
/// </summary>
public sealed class ChatSessionStore
{
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();
    private readonly IOptions<VertexAiOptions> _options;

    public ChatSessionStore(IOptions<VertexAiOptions> options) => _options = options;

    public ChatSession GetOrCreate(string sessionId) =>
        _sessions.GetOrAdd(sessionId, _ => new ChatSession(_options.Value.MaxHistoryTurns));

    public void Clear(string sessionId) =>
        _sessions.TryRemove(sessionId, out _);

    public IEnumerable<string> GetActiveSessions() => _sessions.Keys;
}
