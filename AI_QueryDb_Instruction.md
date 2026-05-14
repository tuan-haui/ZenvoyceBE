# Vertex AI Chat Module — Hướng dẫn đầy đủ cho Agent

> **Phạm vi tài liệu:** Toàn bộ module chat với Google Vertex AI (Gemini) bao gồm
> Streaming · Memory (lịch sử hội thoại) · Function Calling (query DB / tool use)

---

## Mục lục

1. [Kiến trúc tổng quan](#1-kiến-trúc-tổng-quan)
2. [Cấu hình & DI](#2-cấu-hình--di)
3. [Authentication](#3-authentication)
4. [DTOs & Models](#4-dtos--models)
5. [Streaming — Quy tắc bắt buộc](#5-streaming--quy-tắc-bắt-buộc)
6. [Memory — Lịch sử hội thoại](#6-memory--lịch-sử-hội-thoại)
7. [Function Calling — Query DB](#7-function-calling--query-db)
8. [Service hoàn chỉnh](#8-service-hoàn-chỉnh)
9. [Controller](#9-controller)
10. [Checklist](#10-checklist)
11. [Lỗi thường gặp](#11-lỗi-thường-gặp)

---

## 1. Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────┐
│  Client (Browser / Mobile)                                  │
│  - Gửi: { sessionId, message }                              │
│  - Nhận: SSE stream từng chunk text                         │
└───────────────────┬─────────────────────────────────────────┘
                    │ text/event-stream (SSE)
                    ▼
┌─────────────────────────────────────────────────────────────┐
│  ASP.NET Core Controller                                    │
│  - Set headers SSE                                          │
│  - FlushAsync() sau mỗi chunk                               │
└───────────────────┬─────────────────────────────────────────┘
                    │ IAsyncEnumerable<string>
                    ▼
┌─────────────────────────────────────────────────────────────┐
│  VertexAiChatService                                        │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │  Streaming  │  │    Memory    │  │ Function Calling │   │
│  │  ?alt=sse   │  │ ChatSession  │  │  Agentic Loop    │   │
│  │  SSE parse  │  │  History[]   │  │  Tool Executor   │   │
│  └─────────────┘  └──────────────┘  └──────────────────┘   │
└──────────┬──────────────────────────────────┬───────────────┘
           │ HTTP POST ?alt=sse               │ SQL
           ▼                                  ▼
┌──────────────────────┐          ┌───────────────────────┐
│  Vertex AI API       │          │  Database             │
│  (Gemini model)      │          │  (SQL Server / PG)    │
└──────────────────────┘          └───────────────────────┘
```

### Flow agentic loop (có Function Calling)

```
User message
    │
    ▼
Gửi history + tools lên Vertex AI
    │
    ├─► Model trả về functionCall?
    │       │ YES
    │       ▼
    │   Thực thi tool (query DB)
    │       │
    │       ▼
    │   Thêm functionResponse vào history
    │       │
    │       └──► Lặp lại (gửi lên Vertex AI tiếp)
    │
    └─► Model trả về text
            │
            ▼
        Stream từng chunk → client
        Lưu vào history
        KẾT THÚC
```

---

## 2. Cấu hình & DI

### Options

```csharp
public class VertexAiOptions
{
    public const string Section = "VertexAI";

    public string ProjectId { get; init; } = string.Empty;
    public string Model     { get; init; } = "gemini-2.0-flash-001";
    public string Location  { get; init; } = "global";

    // Giới hạn lịch sử để tránh vượt context window
    public int MaxHistoryTurns { get; init; } = 20;
}
```

```json
// appsettings.json
{
  "VertexAI": {
    "ProjectId": "your-gcp-project-id",
    "Model": "gemini-2.0-flash-001",
    "Location": "global",
    "MaxHistoryTurns": 20
  }
}
```

### Đăng ký DI

```csharp
// Program.cs
builder.Services.Configure<VertexAiOptions>(
    builder.Configuration.GetSection(VertexAiOptions.Section));

// HttpClient cho Vertex AI
builder.Services.AddHttpClient<VertexAiChatService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Timeout dài cho stream
});

// Services
builder.Services.AddScoped<IVertexAiChatService, VertexAiChatService>();
builder.Services.AddScoped<ToolExecutor>();

// Session store (in-memory, thay bằng Redis cho production)
builder.Services.AddSingleton<ChatSessionStore>();
```

---

## 3. Authentication

```csharp
// Tìm service-account.json từ thư mục gốc đi lên
private static string? ResolveCredentialPath()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null)
    {
        var candidate = Path.Combine(current.FullName, "DLL", "service-account.json");
        if (File.Exists(candidate)) return candidate;
        current = current.Parent;
    }
    return null;
}

// Cache token để tránh gọi mỗi request (token hết hạn sau ~1 giờ)
private string? _cachedToken;
private DateTime _tokenExpiry = DateTime.MinValue;

private async Task<string> GetAccessTokenAsync()
{
    if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
        return _cachedToken;

    var path = ResolveCredentialPath();
    var credential = path is not null
        ? GoogleCredential.FromFile(path)
        : await GoogleCredential.GetApplicationDefaultAsync();

    var scoped = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
    _cachedToken  = await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
    _tokenExpiry  = DateTime.UtcNow.AddMinutes(55); // Refresh trước 5 phút
    return _cachedToken;
}
```

> **Lưu ý:** Với multi-instance deployment, dùng `IMemoryCache` hoặc `IDistributedCache`
> thay cho field private để share token giữa các instances.

---

## 4. DTOs & Models

### ChatSession — lưu lịch sử hội thoại

```csharp
public class ChatSession
{
    private readonly int _maxTurns;

    public ChatSession(int maxTurns = 20) => _maxTurns = maxTurns;

    public List<ChatTurn> History { get; } = new();

    // User gửi text
    public void AddUser(string text) => AddAndTrim(new ChatTurn
    {
        Role  = "user",
        Parts = new object[] { new { text } }
    });

    // Model trả về text
    public void AddModel(string text) => History.Add(new ChatTurn
    {
        Role  = "model",
        Parts = new object[] { new { text } }
    });

    // Model yêu cầu gọi tool  (role = "model")
    public void AddFunctionCall(string name, JsonElement args) => History.Add(new ChatTurn
    {
        Role  = "model",
        Parts = new object[]
        {
            new { functionCall = new { name, args } }
        }
    });

    // Kết quả tool trả về  (role = "tool")
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

    // Xoá history, giữ lại N turn gần nhất (mỗi turn = 1 cặp user+model)
    private void AddAndTrim(ChatTurn turn)
    {
        History.Add(turn);
        while (History.Count > _maxTurns * 2)
            History.RemoveRange(0, 2); // Xoá cặp cũ nhất
    }
}

public class ChatTurn
{
    public string   Role  { get; init; } = string.Empty;
    public object[] Parts { get; init; } = Array.Empty<object>();
}
```

### ChatSessionStore — quản lý nhiều session

```csharp
public class ChatSessionStore
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
```

### Response DTOs

```csharp
// Response cho non-stream endpoint
public class AiChatResponseDto
{
    public string       Text         { get; init; } = string.Empty;
    public string       Model        { get; init; } = string.Empty;
    public string       FinishReason { get; init; } = string.Empty;
    public AiUsageDto?  Usage        { get; init; }
}

public class AiUsageDto
{
    public int? PromptTokens     { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens      { get; init; }
}
```

---

## 5. Streaming — Quy tắc bắt buộc

> Agent **không được** bỏ qua bất kỳ quy tắc nào dưới đây.

| # | Quy tắc | Lý do |
|---|---------|-------|
| 1 | URL phải có `?alt=sse` | Không có param → API trả về JSON array, không phải SSE stream |
| 2 | `HttpCompletionOption.ResponseHeadersRead` khi `SendAsync` | Không có → HttpClient buffer toàn bộ body, mất tác dụng stream |
| 3 | Đọc bằng `StreamReader` trên `ReadAsStreamAsync` | `ReadAsStringAsync` đợi hết body → không stream được |
| 4 | Strip prefix `data: ` trước khi parse JSON | Format SSE chuẩn, bỏ qua dòng không có prefix này |
| 5 | `yield return` từng chunk ngay, không buffer | Đẩy data xuống client tức thì |
| 6 | `FlushAsync()` sau mỗi `WriteAsync` ở Controller | Không flush → ASP.NET/Nginx giữ lại trong buffer |
| 7 | Header `X-Accel-Buffering: no` ở Controller | Nginx reverse proxy có thể gộp chunks nếu thiếu header này |

### Core SSE reader (dùng chung cho mọi request stream)

```csharp
private async IAsyncEnumerable<JsonElement> ReadSseChunksAsync(
    HttpResponseMessage response,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // [QUY TẮC 3]
    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(line)) continue;

        // [QUY TẮC 4]
        if (!line.StartsWith("data: ")) continue;

        var json = line["data: ".Length..];
        if (json == "[DONE]") break;

        JsonDocument? doc = null;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException) { continue; }

        using (doc)
            yield return doc.RootElement.Clone(); // Clone để dùng ngoài using
    }
}
```

---

## 6. Memory — Lịch sử hội thoại

**Nguyên tắc:** Vertex AI không tự lưu trạng thái. Mỗi request phải gửi
**toàn bộ** `history` lên API theo thứ tự đúng.

### Thứ tự role hợp lệ trong history

```
user → model → user → model → ...         (chat bình thường)
user → model(functionCall) → tool → model  (khi dùng function calling)
```

> **Quan trọng:** Vertex AI dùng role `"model"` (không phải `"assistant"` như OpenAI).
> Role `"tool"` dành riêng cho kết quả function calling.

### Cách build payload với history

```csharp
var payload = new
{
    contents = session.History.Select(turn => new
    {
        role  = turn.Role,
        parts = turn.Parts
    }).ToArray(),
    tools = VertexAiTools.Definitions  // Bỏ nếu không dùng function calling
};
```

---

## 7. Function Calling — Query DB

### 7.1 Khai báo Tools

```csharp
public static class VertexAiTools
{
    public static object[] Definitions => new[]
    {
        new
        {
            functionDeclarations = new object[]
            {
                new
                {
                    name        = "get_revenue_report",
                    description = "Lấy báo cáo doanh thu theo tháng, năm, chi nhánh",
                    parameters  = new
                    {
                        type       = "object",
                        properties = new
                        {
                            month     = new { type = "integer", description = "Tháng (1-12)" },
                            year      = new { type = "integer", description = "Năm" },
                            branch_id = new { type = "string",  description = "Mã chi nhánh. Bỏ trống = tất cả" }
                        },
                        required = new[] { "month", "year" }
                    }
                },
                new
                {
                    name        = "get_risk_assessment",
                    description = "Đánh giá rủi ro tín dụng của khách hàng dựa trên lịch sử hợp đồng",
                    parameters  = new
                    {
                        type       = "object",
                        properties = new
                        {
                            customer_id = new { type = "string", description = "Mã khách hàng" },
                            contract_id = new { type = "string", description = "Mã hợp đồng cụ thể (tuỳ chọn)" }
                        },
                        required = new[] { "customer_id" }
                    }
                },
                new
                {
                    name        = "get_overdue_contracts",
                    description = "Danh sách hợp đồng quá hạn, lọc theo số ngày và chi nhánh",
                    parameters  = new
                    {
                        type       = "object",
                        properties = new
                        {
                            days_overdue = new { type = "integer", description = "Số ngày quá hạn tối thiểu" },
                            branch_id    = new { type = "string",  description = "Mã chi nhánh (tuỳ chọn)" }
                        },
                        required = new[] { "days_overdue" }
                    }
                }
            }
        }
    };
}
```

### 7.2 Tool Executor

```csharp
public class ToolExecutor
{
    private readonly IDbConnection _db;

    public ToolExecutor(IDbConnection db) => _db = db;

    public async Task<object> ExecuteAsync(string toolName, JsonElement args)
    {
        return toolName switch
        {
            "get_revenue_report"    => await GetRevenueReportAsync(args),
            "get_risk_assessment"   => await GetRiskAssessmentAsync(args),
            "get_overdue_contracts" => await GetOverdueContractsAsync(args),
            _ => throw new NotSupportedException($"Unknown tool: {toolName}")
        };
    }

    private async Task<object> GetRevenueReportAsync(JsonElement args)
    {
        var month    = args.GetProperty("month").GetInt32();
        var year     = args.GetProperty("year").GetInt32();
        var branchId = args.TryGetProperty("branch_id", out var b) ? b.GetString() : null;

        var sql = @"
            SELECT branch_id,
                   SUM(amount)  AS revenue,
                   COUNT(*)     AS contracts
            FROM   contracts
            WHERE  MONTH(disbursed_at) = @month
              AND  YEAR(disbursed_at)  = @year
              AND  (@branchId IS NULL OR branch_id = @branchId)
            GROUP  BY branch_id";

        var rows = await _db.QueryAsync(sql, new { month, year, branchId });
        return new { month, year, branch_id = branchId ?? "ALL", data = rows };
    }

    private async Task<object> GetRiskAssessmentAsync(JsonElement args)
    {
        var customerId = args.GetProperty("customer_id").GetString();
        var contractId = args.TryGetProperty("contract_id", out var c) ? c.GetString() : null;

        var customer = await _db.QueryFirstOrDefaultAsync(
            "SELECT id, full_name, credit_score FROM customers WHERE id = @customerId",
            new { customerId });

        var contracts = await _db.QueryAsync(@"
            SELECT id, amount, overdue_days, status
            FROM   contracts
            WHERE  customer_id = @customerId
              AND  (@contractId IS NULL OR id = @contractId)
            ORDER  BY created_at DESC
            LIMIT  10",
            new { customerId, contractId });

        var list = contracts.ToList();
        return new
        {
            customer,
            contracts = list,
            summary = new
            {
                total_contracts  = list.Count,
                overdue_count    = list.Count(x => (int)x.overdue_days > 0),
                max_overdue_days = list.Count > 0 ? list.Max(x => (int)x.overdue_days) : 0
            }
        };
    }

    private async Task<object> GetOverdueContractsAsync(JsonElement args)
    {
        var daysOverdue = args.GetProperty("days_overdue").GetInt32();
        var branchId    = args.TryGetProperty("branch_id", out var b) ? b.GetString() : null;

        var rows = await _db.QueryAsync(@"
            SELECT id, customer_id, amount, overdue_days, branch_id
            FROM   contracts
            WHERE  overdue_days >= @daysOverdue
              AND  (@branchId IS NULL OR branch_id = @branchId)
            ORDER  BY overdue_days DESC",
            new { daysOverdue, branchId });

        return new { days_overdue = daysOverdue, contracts = rows };
    }
}
```

---

## 8. Service hoàn chỉnh

```csharp
public interface IVertexAiChatService
{
    IAsyncEnumerable<string> ChatStreamAsync(
        ChatSession session,
        string newMessage,
        CancellationToken cancellationToken = default);
}

public class VertexAiChatService : IVertexAiChatService
{
    private readonly HttpClient          _httpClient;
    private readonly VertexAiOptions     _options;
    private readonly ToolExecutor        _toolExecutor;

    // Token cache
    private string?  _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public VertexAiChatService(
        HttpClient              httpClient,
        IOptions<VertexAiOptions> options,
        ToolExecutor            toolExecutor)
    {
        _httpClient   = httpClient;
        _options      = options.Value;
        _toolExecutor = toolExecutor;
    }

    // ─── Public: Stream với memory + function calling ────────────────────────
    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatSession session,
        string newMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        session.AddUser(newMessage);

        // Agentic loop — lặp cho đến khi model trả về text (không còn gọi tool)
        while (true)
        {
            var response = await SendRequestAsync(session, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Vertex AI error {(int)response.StatusCode}: {error}");
            }

            // Với function calling, cần đọc full response để kiểm tra functionCall
            // trước khi stream text về client
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc      = JsonDocument.Parse(rawJson);
            var root           = doc.RootElement;
            var isSseArray     = root.ValueKind == JsonValueKind.Array;
            var firstChunk     = isSseArray ? root[0] : root;
            var candidate      = GetFirstCandidate(firstChunk);
            var parts          = candidate.TryGetProperty("content", out var content)
                                 && content.TryGetProperty("parts", out var p) ? p : default;

            // Kiểm tra functionCall
            var functionCallPart = parts.ValueKind == JsonValueKind.Array
                ? parts.EnumerateArray()
                       .FirstOrDefault(x => x.TryGetProperty("functionCall", out _))
                : default;

            if (functionCallPart.ValueKind != JsonValueKind.Undefined)
            {
                var fc       = functionCallPart.GetProperty("functionCall");
                var toolName = fc.GetProperty("name").GetString()!;
                var toolArgs = fc.GetProperty("args");

                // Thông báo cho user biết đang xử lý
                yield return $"\n⚙️ *Đang truy vấn: {toolName}...*\n";

                // Lưu functionCall vào history
                session.AddFunctionCall(toolName, toolArgs);

                // Thực thi tool
                var result = await _toolExecutor.ExecuteAsync(toolName, toolArgs);

                // Lưu kết quả vào history
                session.AddFunctionResponse(toolName, result);

                // Tiếp tục vòng lặp — gửi lại lên model kèm kết quả
                continue;
            }

            // Model trả về text → stream từng chunk
            var fullText = new StringBuilder();

            if (isSseArray)
            {
                // Response là mảng SSE chunks
                foreach (var chunk in root.EnumerateArray())
                {
                    var text = ExtractText(chunk);
                    if (!string.IsNullOrEmpty(text))
                    {
                        fullText.Append(text);
                        yield return text;
                    }
                }
            }
            else
            {
                var text = ExtractText(root);
                if (!string.IsNullOrEmpty(text))
                {
                    fullText.Append(text);
                    yield return text;
                }
            }

            // Lưu response của model vào history
            session.AddModel(fullText.ToString());
            break; // Kết thúc agentic loop
        }
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    // [QUY TẮC 1] URL có ?alt=sse
    // [QUY TẮC 2] HttpCompletionOption.ResponseHeadersRead
    private async Task<HttpResponseMessage> SendRequestAsync(
        ChatSession session,
        CancellationToken cancellationToken)
    {
        // Với function calling, dùng endpoint không có ?alt=sse để nhận JSON đầy đủ
        // giúp phát hiện functionCall trước khi stream
        var hasTools = true; // Đặt false nếu không dùng function calling
        var endpoint = $"https://aiplatform.googleapis.com/v1/projects/{_options.ProjectId}" +
                       $"/locations/global/publishers/google/models/{_options.Model}" +
                       $":streamGenerateContent" +
                       (hasTools ? "" : "?alt=sse");

        var payload = new
        {
            contents = session.History.Select(t => new { role = t.Role, parts = t.Parts }).ToArray(),
            tools    = hasTools ? VertexAiTools.Definitions : null
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        return await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead, // [QUY TẮC 2]
            cancellationToken);
    }

    private static string? ExtractText(JsonElement root)
    {
        var candidate = GetFirstCandidate(root);
        if (candidate.ValueKind == JsonValueKind.Undefined) return null;
        if (!candidate.TryGetProperty("content", out var content)) return null;
        if (!content.TryGetProperty("parts", out var parts)) return null;

        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
            if (part.TryGetProperty("text", out var t))
                sb.Append(t.GetString());

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static JsonElement GetFirstCandidate(JsonElement root)
    {
        return root.TryGetProperty("candidates", out var candidates)
               && candidates.ValueKind == JsonValueKind.Array
            ? candidates.EnumerateArray().FirstOrDefault()
            : default;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var path = ResolveCredentialPath();
        var credential = path is not null
            ? GoogleCredential.FromFile(path)
            : await GoogleCredential.GetApplicationDefaultAsync();

        var scoped   = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        _cachedToken = await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
        return _cachedToken;
    }

    private static string? ResolveCredentialPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "DLL", "service-account.json");
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        return null;
    }
}
```

---

## 9. Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class AiChatController : ControllerBase
{
    private readonly IVertexAiChatService _chatService;
    private readonly ChatSessionStore     _sessionStore;

    public AiChatController(
        IVertexAiChatService chatService,
        ChatSessionStore     sessionStore)
    {
        _chatService  = chatService;
        _sessionStore = sessionStore;
    }

    /// <summary>
    /// Stream chat với memory và function calling.
    /// Client nhận SSE: data: "chunk text"\n\n ... data: [DONE]\n\n
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamAsync(
        [FromQuery] string sessionId,
        [FromQuery] string message,
        CancellationToken  cancellationToken)
    {
        // [QUY TẮC 6, 7] Headers SSE
        Response.Headers["Content-Type"]      = "text/event-stream";
        Response.Headers["Cache-Control"]     = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // Tắt Nginx buffer

        var session = _sessionStore.GetOrCreate(sessionId);

        await foreach (var chunk in _chatService.ChatStreamAsync(session, message, cancellationToken))
        {
            var data = $"data: {JsonSerializer.Serialize(chunk)}\n\n";
            await Response.WriteAsync(data, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken); // [QUY TẮC 6]
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Xoá lịch sử hội thoại của session.
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    public IActionResult ClearSession(string sessionId)
    {
        _sessionStore.Clear(sessionId);
        return NoContent();
    }
}
```

---

## 10. Checklist

### Streaming
- [ ] URL có `?alt=sse` (khi không dùng function calling)
- [ ] `SendAsync` dùng `HttpCompletionOption.ResponseHeadersRead`
- [ ] Đọc bằng `StreamReader` trên `ReadAsStreamAsync`, không `ReadAsStringAsync`
- [ ] Strip prefix `data: ` trước khi parse JSON
- [ ] `yield return` từng chunk ngay, không buffer
- [ ] Controller set `Content-Type: text/event-stream`
- [ ] Controller gọi `FlushAsync()` sau mỗi chunk
- [ ] Header `X-Accel-Buffering: no` nếu deploy sau Nginx
- [ ] `[EnumeratorCancellation]` trên `CancellationToken` của `IAsyncEnumerable`

### Memory
- [ ] Mỗi request gửi toàn bộ `session.History` lên API
- [ ] Role dùng đúng: `"user"` / `"model"` / `"tool"` (không phải `"assistant"`)
- [ ] Lưu response của model vào history sau khi stream xong (`session.AddModel`)
- [ ] Giới hạn `MaxHistoryTurns` để tránh vượt context window

### Function Calling
- [ ] Khai báo đầy đủ `functionDeclarations` với `description` rõ ràng (model dựa vào đây để quyết định)
- [ ] `required` fields được khai báo đúng trong schema
- [ ] Agentic loop tiếp tục gửi request khi nhận `functionCall`, không break
- [ ] Lưu `functionCall` (role: model) **trước** khi execute tool
- [ ] Lưu `functionResponse` (role: tool) **sau** khi execute xong
- [ ] Tool executor có try-catch, không để exception làm crash loop
- [ ] Tất cả tên tool trong `ToolExecutor.ExecuteAsync` khớp với `name` trong Definitions

### Production
- [ ] Cache access token (55 phút)
- [ ] HttpClient timeout đủ dài (≥ 3 phút) cho các query phức tạp
- [ ] `ChatSessionStore` dùng Redis/Distributed cache (không dùng in-memory singleton)
- [ ] Giới hạn độ dài `message` đầu vào để tránh abuse

---

## 11. Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Fix |
|---|---|---|
| Vẫn chờ hết mới nhận data | Thiếu `?alt=sse` hoặc thiếu `ResponseHeadersRead` | Thêm cả hai |
| Client nhận data theo batch chậm | Thiếu `FlushAsync()` | Flush sau mỗi `WriteAsync` |
| Nginx gộp chunks | Nginx reverse proxy buffer | Thêm `X-Accel-Buffering: no` |
| `400 Bad Request` khi gửi history | Sai thứ tự role hoặc dùng `"assistant"` thay `"model"` | Kiểm tra role trong `ChatTurn` |
| Model không gọi tool dù hỏi đúng | `description` của tool không rõ | Viết lại description cụ thể hơn |
| Agentic loop vô hạn | Tool luôn fail, model cứ retry | Thêm `maxIterations` counter, throw nếu vượt |
| `JsonException` khi parse chunk | Dòng SSE trống hoặc comment | Đã xử lý bằng `if (!line.StartsWith("data: ")) continue` |
| Access token hết hạn | Token cache không hoạt động | Kiểm tra logic `_tokenExpiry` |
| History quá dài → `413` hoặc chậm | Không trim history | Đặt `MaxHistoryTurns`, gọi `Trim()` |