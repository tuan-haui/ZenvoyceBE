using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Ai.DTOs;
using Zenvoyce.Infrastructure.Options;

namespace Zenvoyce.Infrastructure.Services.Ai;

/// <summary>
/// Vertex AI Chat Service hỗ trợ:
///   - Memory: gửi toàn bộ lịch sử hội thoại mỗi request
///   - Function Calling: agentic loop tự động gọi tool khi model yêu cầu
///   - Streaming: yield return từng chunk text về controller
/// </summary>
public sealed class VertexAiChatService : IVertexAiChatService
{
    private readonly HttpClient            _httpClient;
    private readonly VertexAiOptions       _options;
    private readonly ToolExecutor          _toolExecutor;
    private readonly ILogger<VertexAiChatService> _logger;

    // Token cache (token hết hạn sau ~1 giờ, refresh trước 5 phút)
    private string?  _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    // Giới hạn số vòng lặp agentic để tránh loop vô hạn
    private const int MaxAgenticIterations = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // System instruction — giúp model hiểu ngữ cảnh hệ thống Zenvoyce
    private const string SystemInstruction =
        "Bạn là trợ lý AI của hệ thống Zenvoyce — phần mềm quản lý hóa đơn điện tử. " +
        "Bạn có thể truy vấn dữ liệu hóa đơn trực tiếp từ database để trả lời các câu hỏi về: " +
        "thống kê doanh thu, danh sách hóa đơn theo khách hàng, hóa đơn theo trạng thái, chi tiết hóa đơn cụ thể, " +
        "và đánh giá rủi ro hóa đơn (dùng get_invoices_for_risk_assessment với limit phù hợp). " +
        "Hãy trả lời bằng tiếng Việt, ngắn gọn và chuyên nghiệp. " +
        "Khi cần số liệu từ database, hãy gọi tool phù hợp thay vì tự đoán.";

    public VertexAiChatService(
        HttpClient                        httpClient,
        IOptions<VertexAiOptions>         options,
        ToolExecutor                      toolExecutor,
        ILogger<VertexAiChatService>      logger)
    {
        _httpClient   = httpClient;
        _options      = options.Value;
        _toolExecutor = toolExecutor;
        _logger       = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── Public: Stream với memory + function calling ─────────────────────────

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatSession session,
        string newMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        session.AddUser(newMessage);

        var iterations = 0;

        // Agentic loop — lặp cho đến khi model trả về text (không còn gọi tool)
        while (true)
        {
            if (++iterations > MaxAgenticIterations)
            {
                yield return "\n⚠️ *Vượt quá số vòng lặp tối đa. Vui lòng thử lại.*";
                break;
            }

            var response = await SendRequestAsync(session, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Vertex AI error {(int)response.StatusCode}: {error}");
            }

            // Đọc toàn bộ JSON response để kiểm tra functionCall trước khi stream text
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc      = JsonDocument.Parse(rawJson);
            var root           = doc.RootElement;

            // Vertex AI trả về JSON array khi dùng streamGenerateContent (không có ?alt=sse)
            var isSseArray     = root.ValueKind == JsonValueKind.Array;
            var firstChunk     = isSseArray ? root[0] : root;
            var candidate      = GetFirstCandidate(firstChunk);

            // Kiểm tra functionCall — lấy parts từ first candidate
            var functionCallPart = GetFunctionCallPart(candidate);


            if (functionCallPart.ValueKind != JsonValueKind.Undefined)
            {
                var fc       = functionCallPart.GetProperty("functionCall");
                var toolName = fc.GetProperty("name").GetString()!;
                var toolArgs = fc.GetProperty("args");

                // Thông báo ngắn cho user biết đang xử lý
                yield return $"\n⚙️ *Đang truy vấn: {toolName}...*\n";

                _logger.LogInformation("[VertexAiChatService] Calling tool: {ToolName}", toolName);

                // Lưu functionCall vào history (role: model — TRƯỚC khi execute)
                session.AddFunctionCall(toolName, toolArgs.Clone());

                // Thực thi tool
                var result = await _toolExecutor.ExecuteAsync(toolName, toolArgs);

                // Kiểm tra nếu tool trả về error (serialize to JSON để check)
                var resultJson = System.Text.Json.JsonSerializer.Serialize(result, JsonOptions);
                using var resultDoc = JsonDocument.Parse(resultJson);
                if (resultDoc.RootElement.TryGetProperty("error", out var errorProp))
                {
                    var errorMsg = errorProp.GetString() ?? "Lỗi không xác định";
                    _logger.LogError("[VertexAiChatService] Tool {ToolName} failed: {Error}", toolName, errorMsg);
                }
                else
                {
                    _logger.LogInformation("[VertexAiChatService] Tool {ToolName} succeeded", toolName);
                }

                // Lưu kết quả vào history (role: tool — SAU khi execute)
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
            if (fullText.Length > 0)
                session.AddModel(fullText.ToString());

            break; // Kết thúc agentic loop
        }
    }

    // ─── Private: Gửi request lên Vertex AI ──────────────────────────────────

    private async Task<HttpResponseMessage> SendRequestAsync(
        ChatSession session,
        CancellationToken cancellationToken)
    {
        // Dùng streamGenerateContent (không có ?alt=sse) để nhận JSON array
        // — giúp phát hiện functionCall trong toàn bộ response trước khi stream
        var endpoint = $"https://aiplatform.googleapis.com/v1/projects/{_options.ProjectId}" +
                       $"/locations/{_options.Location}/publishers/google/models/{_options.Model}" +
                       $":streamGenerateContent";

        var payload = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = SystemInstruction } }
            },
            contents = session.History
                .Select(t => new { role = t.Role, parts = t.Parts })
                .ToArray(),
            tools = VertexAiTools.Definitions
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

        // ResponseHeadersRead để không buffer toàn bộ body
        return await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    // ─── Private: Extract text từ chunk ──────────────────────────────────────

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

    private static JsonElement GetFunctionCallPart(JsonElement candidate)
    {
        if (candidate.ValueKind == JsonValueKind.Undefined) return default;
        if (!candidate.TryGetProperty("content", out var content)) return default;
        if (!content.TryGetProperty("parts", out var parts)) return default;
        if (parts.ValueKind != JsonValueKind.Array) return default;

        foreach (var part in parts.EnumerateArray())
            if (part.TryGetProperty("functionCall", out _))
                return part;

        return default;
    }


    // ─── Private: Access token với cache ─────────────────────────────────────

    private async Task<string> GetAccessTokenAsync()
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var path       = ResolveCredentialPath();
        var credential = path is not null
            ? GoogleCredential.FromFile(path)
            : await GoogleCredential.GetApplicationDefaultAsync();

        var scoped   = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        _cachedToken = await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _tokenExpiry = DateTime.UtcNow.AddMinutes(55); // Refresh trước khi hết hạn 5 phút
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
