# Vertex AI Streaming — Hướng dẫn cho Agent

## Mục tiêu

Implement module chat với Google Vertex AI (`streamGenerateContent`) trả về **real-time stream** từng chunk text thay vì chờ model generate xong toàn bộ.

---

## Kiến trúc tổng quan

```
Client (Browser/App)
    │  SSE / IAsyncEnumerable
    ▼
ASP.NET Core Controller
    │  IAsyncEnumerable<string>
    ▼
VertexAiChatService.ChatStreamAsync()
    │  HttpCompletionOption.ResponseHeadersRead + SSE parse
    ▼
Vertex AI API  (?alt=sse endpoint)
```

---

## Quy tắc bắt buộc (không được bỏ qua)

| # | Quy tắc | Lý do |
|---|---------|-------|
| 1 | URL phải có `?alt=sse` | Không có param này, API trả về JSON array thay vì SSE stream |
| 2 | `HttpCompletionOption.ResponseHeadersRead` | Không có option này, HttpClient vẫn buffer toàn bộ body trước khi trả về |
| 3 | Đọc stream bằng `StreamReader`, **không** dùng `ReadAsStringAsync` | `ReadAsStringAsync` đợi hết body → mất tác dụng stream |
| 4 | Mỗi dòng SSE format: `data: {json}` | Phải strip prefix `data: ` trước khi parse JSON |
| 5 | `yield return` từng chunk ngay khi có | Đẩy dữ liệu xuống client ngay, không buffer |
| 6 | `FlushAsync()` sau mỗi lần write ở Controller | Không flush thì response bị giữ trong buffer của ASP.NET/Nginx |

---

## Service Layer

### Interface

```csharp
public interface IVertexAiChatService
{
    // Non-stream (giữ lại cho các use case cần full response)
    Task<AiChatResponseDto> ChatAsync(string message, CancellationToken cancellationToken = default);

    // Stream — trả về từng chunk text ngay khi có
    IAsyncEnumerable<string> ChatStreamAsync(string message, CancellationToken cancellationToken = default);
}
```

### ChatStreamAsync — Implementation đầy đủ

```csharp
public async IAsyncEnumerable<string> ChatStreamAsync(
    string message,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // [QUY TẮC 1] Thêm ?alt=sse để nhận SSE thay vì JSON array
    var endpoint =
        $"https://aiplatform.googleapis.com/v1/projects/{_options.ProjectId}" +
        $"/locations/global/publishers/google/models/{_options.Model}" +
        $":streamGenerateContent?alt=sse";

    var payload = new
    {
        contents = new[]
        {
            new
            {
                role = "user",
                parts = new[] { new { text = message } }
            }
        }
    };

    var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json")
    };
    request.Headers.Authorization =
        new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

    // [QUY TẮC 2] ResponseHeadersRead — nhận response ngay khi có headers, không chờ body
    using var response = await httpClient.SendAsync(
        request,
        HttpCompletionOption.ResponseHeadersRead,
        cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Vertex AI request failed with status {(int)response.StatusCode}: {error}");
    }

    // [QUY TẮC 3] Đọc stream trực tiếp, không ReadAsStringAsync
    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(line)) continue;

        // [QUY TẮC 4] Strip prefix SSE
        if (!line.StartsWith("data: ")) continue;

        var json = line["data: ".Length..];

        if (json == "[DONE]") break;

        string? text = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            text = ExtractTextFromChunk(doc.RootElement);
        }
        catch (JsonException)
        {
            continue; // Bỏ qua chunk không parse được
        }

        // [QUY TẮC 5] yield return ngay, không buffer
        if (!string.IsNullOrEmpty(text))
            yield return text;
    }
}
```

### Helper — ExtractTextFromChunk

```csharp
private static string? ExtractTextFromChunk(JsonElement chunk)
{
    if (!chunk.TryGetProperty("candidates", out var candidates)
        || candidates.ValueKind != JsonValueKind.Array)
        return null;

    var sb = new StringBuilder();

    foreach (var candidate in candidates.EnumerateArray())
    {
        if (!candidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array)
            continue;

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textEl))
                sb.Append(textEl.GetString());
        }
    }

    return sb.Length > 0 ? sb.ToString() : null;
}
```

---

## Controller Layer

### Cách 1: Server-Sent Events (SSE) — Chuẩn nhất cho browser

```csharp
[HttpGet("stream")]
public async Task StreamSse(
    [FromQuery] string message,
    CancellationToken cancellationToken)
{
    Response.Headers["Content-Type"]      = "text/event-stream";
    Response.Headers["Cache-Control"]     = "no-cache";
    Response.Headers["X-Accel-Buffering"] = "no"; // Tắt Nginx buffer

    await foreach (var chunk in _chatService.ChatStreamAsync(message, cancellationToken))
    {
        // Format SSE chuẩn
        var data = $"data: {JsonSerializer.Serialize(chunk)}\n\n";
        await Response.WriteAsync(data, cancellationToken);

        // [QUY TẮC 6] Flush ngay để client nhận được chunk
        await Response.Body.FlushAsync(cancellationToken);
    }

    await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    await Response.Body.FlushAsync(cancellationToken);
}
```

### Cách 2: IAsyncEnumerable trực tiếp (dành cho client .NET / gRPC-style)

```csharp
[HttpGet("stream-json")]
public async IAsyncEnumerable<string> StreamJson(
    [FromQuery] string message,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var chunk in _chatService.ChatStreamAsync(message, cancellationToken))
    {
        yield return chunk;
    }
}
```

---

## DTOs

```csharp
// Response đầy đủ cho non-stream endpoint
public class AiChatResponseDto
{
    public string Text          { get; init; } = string.Empty;
    public string Model         { get; init; } = string.Empty;
    public string FinishReason  { get; init; } = string.Empty;
    public AiUsageDto? Usage    { get; init; }
}

public class AiUsageDto
{
    public int? PromptTokens     { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens      { get; init; }
}
```

---

## Cấu hình Options

```csharp
public class VertexAiOptions
{
    public const string Section = "VertexAI";

    public string ProjectId { get; init; } = string.Empty;
    public string Model     { get; init; } = "gemini-2.0-flash-001"; // hoặc model khác
    public string Location  { get; init; } = "global";
}
```

```json
// appsettings.json
{
  "VertexAI": {
    "ProjectId": "your-gcp-project-id",
    "Model": "gemini-2.0-flash-001",
    "Location": "global"
  }
}
```

---

## Đăng ký DI

```csharp
// Program.cs
builder.Services.Configure<VertexAiOptions>(
    builder.Configuration.GetSection(VertexAiOptions.Section));

builder.Services.AddHttpClient<VertexAiChatService>();
builder.Services.AddScoped<IVertexAiChatService, VertexAiChatService>();
```

---

## Authentication — GetAccessTokenAsync

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

private static async Task<string> GetAccessTokenAsync()
{
    var path = ResolveCredentialPath();
    var credential = path is not null
        ? GoogleCredential.FromFile(path)
        : await GoogleCredential.GetApplicationDefaultAsync();

    var scoped = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
    return await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
}
```

> **Lưu ý production:** Cache access token (hết hạn sau ~1 giờ), không gọi `GetAccessTokenAsync()` mỗi request.

---

## Checklist trước khi code

- [ ] URL có `?alt=sse`
- [ ] `SendAsync` dùng `HttpCompletionOption.ResponseHeadersRead`
- [ ] Đọc bằng `StreamReader` trên `ReadAsStreamAsync`
- [ ] Parse từng dòng, strip prefix `data: `
- [ ] `yield return` từng chunk, không buffer
- [ ] Controller set header `Content-Type: text/event-stream`
- [ ] Controller gọi `FlushAsync()` sau mỗi chunk
- [ ] Header `X-Accel-Buffering: no` nếu deploy sau Nginx
- [ ] `[EnumeratorCancellation]` trên `CancellationToken` của `IAsyncEnumerable`

---

## Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Fix |
|---|---|---|
| Vẫn chờ hết rồi mới nhận được data | Thiếu `?alt=sse` hoặc thiếu `ResponseHeadersRead` | Thêm cả hai |
| Client nhận được data nhưng chậm theo từng batch | Thiếu `FlushAsync()` ở controller | Thêm flush sau mỗi write |
| Nginx gộp chunks lại | Buffer của reverse proxy | Thêm `X-Accel-Buffering: no` |
| `JsonException` trên dòng đầu tiên | Dòng SSE comment (`": "` hoặc blank) | Đã xử lý bằng `if (!line.StartsWith("data: ")) continue` |
| Token hết hạn giữa chừng | Access token cache 1 giờ | Implement token cache với refresh |