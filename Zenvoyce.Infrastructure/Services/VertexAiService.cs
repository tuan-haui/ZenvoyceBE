using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Ai.DTOs;
using Zenvoyce.Infrastructure.Options;

namespace Zenvoyce.Infrastructure.Services;

public sealed class VertexAiService(
    HttpClient httpClient,
    IOptions<VertexAiOptions> options) : IVertexAiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly VertexAiOptions _options = options.Value;

    public async Task<AiChatResponseDto> ChatAsync(string message, CancellationToken cancellationToken)
    {
        var endpoint =
            $"https://aiplatform.googleapis.com/v1/projects/{_options.ProjectId}/locations/global/publishers/google/models/{_options.Model}:streamGenerateContent";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = message }
                    }
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Vertex AI request failed with status {(int)response.StatusCode}: {rawResponse}");
        }

        using var document = JsonDocument.Parse(rawResponse);
        return MapResponse(document.RootElement);
    }

    /// <summary>
    /// Stream response từ Vertex AI - trả về từng chunk text ngay khi có.
    /// </summary>
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

        // [QUY TẮC 2] ResponseHeadersRead - nhận response ngay khi có headers, không chờ body
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

    private static async Task<string> GetAccessTokenAsync()
    {
        var credentialPath = ResolveCredentialPath();
        var credential = credentialPath is not null
            ? GoogleCredential.FromFile(credentialPath)
            : await GoogleCredential.GetApplicationDefaultAsync();
        var scoped = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        return await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
    }

    private static string? ResolveCredentialPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidatePath = Path.Combine(current.FullName, "DLL", "service-account.json");
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            current = current.Parent;
        }

        return null;
    }

    private static AiChatResponseDto MapResponse(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return MapStreamingArrayResponse(root);
        }

        return MapSingleResponse(root);
    }

    private static AiChatResponseDto MapStreamingArrayResponse(JsonElement chunks)
    {
        var textBuilder = new StringBuilder();
        string model = string.Empty;
        string finishReason = string.Empty;
        AiUsageDto? usage = null;

        foreach (var chunk in chunks.EnumerateArray())
        {
            AppendCandidateTexts(chunk, textBuilder);

            if (chunk.TryGetProperty("modelVersion", out var modelVersionElement))
            {
                model = modelVersionElement.GetString() ?? model;
            }

            var candidate = GetFirstCandidate(chunk);
            if (candidate.ValueKind != JsonValueKind.Undefined &&
                candidate.TryGetProperty("finishReason", out var finishReasonElement))
            {
                finishReason = finishReasonElement.GetString() ?? finishReason;
            }

            if (chunk.TryGetProperty("usageMetadata", out var usageMetadata))
            {
                usage = new AiUsageDto
                {
                    PromptTokens = TryGetInt(usageMetadata, "promptTokenCount"),
                    CompletionTokens = TryGetInt(usageMetadata, "candidatesTokenCount"),
                    TotalTokens = TryGetInt(usageMetadata, "totalTokenCount")
                };
            }
        }

        return new AiChatResponseDto
        {
            Text = textBuilder.ToString(),
            Model = model,
            FinishReason = finishReason,
            Usage = usage
        };
    }

    private static AiChatResponseDto MapSingleResponse(JsonElement root)
    {
        var textBuilder = new StringBuilder();
        AppendCandidateTexts(root, textBuilder);

        var candidate = GetFirstCandidate(root);
        var finishReason = candidate.ValueKind != JsonValueKind.Undefined &&
                           candidate.TryGetProperty("finishReason", out var finishReasonElement)
            ? finishReasonElement.GetString() ?? string.Empty
            : string.Empty;

        var model = root.TryGetProperty("modelVersion", out var modelVersionElement)
            ? modelVersionElement.GetString() ?? string.Empty
            : string.Empty;

        AiUsageDto? usage = null;
        if (root.TryGetProperty("usageMetadata", out var usageMetadata))
        {
            usage = new AiUsageDto
            {
                PromptTokens = TryGetInt(usageMetadata, "promptTokenCount"),
                CompletionTokens = TryGetInt(usageMetadata, "candidatesTokenCount"),
                TotalTokens = TryGetInt(usageMetadata, "totalTokenCount")
            };
        }

        return new AiChatResponseDto
        {
            Text = textBuilder.ToString(),
            Model = model,
            FinishReason = finishReason,
            Usage = usage
        };
    }

    private static JsonElement GetFirstCandidate(JsonElement root)
    {
        return root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array
            ? candidates.EnumerateArray().FirstOrDefault()
            : default;
    }

    private static void AppendCandidateTexts(JsonElement root, StringBuilder textBuilder)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                {
                    textBuilder.Append(textElement.GetString() ?? string.Empty);
                }
            }
        }
    }

    private static int? TryGetInt(JsonElement parent, string propertyName)
    {
        if (parent.TryGetProperty(propertyName, out var element) && element.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Trích xuất text từ chunk SSE trong quá trình streaming.
    /// </summary>
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
}
