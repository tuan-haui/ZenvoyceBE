# ⚡ Hướng dẫn code & triển khai Claude (Vertex AI) với Angular + .NET

## 1. Kiến trúc (chuẩn)

```text
Angular (Frontend)
        ↓
.NET Web API (Backend)
        ↓
Vertex AI (Claude Sonnet)
```

---

## 2. Chuẩn bị backend (.NET)

### Tạo project

```bash
dotnet new webapi -n AiBackend
cd AiBackend
```

---

## 3. Cài package cần thiết

Hiện tại Vertex AI chưa có SDK .NET chính thức mạnh như Node, nên ta gọi qua HTTP:

```bash
dotnet add package System.Net.Http.Json
```

---

## 4. Thiết lập authentication

### Cách chuẩn (Service Account)

Set biến môi trường:

```bash
set GOOGLE_APPLICATION_CREDENTIALS=path\to\service-account.json
```

(.NET sẽ tự dùng credential này khi gọi Google API nếu dùng đúng endpoint)

---

## 5. Gọi Vertex AI (Claude) bằng HTTP

### 📌 Endpoint dạng:

```text
POST https://us-central1-aiplatform.googleapis.com/v1/projects/{PROJECT_ID}/locations/us-central1/publishers/anthropic/models/{MODEL}:generateContent
```

---

## 6. Code Backend (.NET)

### `Controllers/ChatController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public ChatController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var projectId = "YOUR_PROJECT_ID";
        var location = "us-central1";
        var model = "claude-3-5-sonnet@20240620";

        var url = $"https://{location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{location}/publishers/anthropic/models/{model}:generateContent";

        var payload = new
        {
            contents = new[]
            {
                new {
                    role = "user",
                    parts = new[] {
                        new { text = request.Message }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Lấy access token từ Google
        var token = await GoogleCredentialHelper.GetAccessToken();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.PostAsync(url, httpContent);
        var result = await response.Content.ReadAsStringAsync();

        return Content(result, "application/json");
    }
}

public class ChatRequest
{
    public string Message { get; set; }
}
```

---

## 7. Helper lấy Access Token

### `GoogleCredentialHelper.cs`

```csharp
using Google.Apis.Auth.OAuth2;

public static class GoogleCredentialHelper
{
    public static async Task<string> GetAccessToken()
    {
        var credential = await GoogleCredential
            .GetApplicationDefaultAsync();

        var scoped = credential.CreateScoped(
            "https://www.googleapis.com/auth/cloud-platform");

        return await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
    }
}
```

---

## 8. Đăng ký HttpClient

### `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapControllers();

app.Run();
```

---

## 9. Test API

```bash
POST http://localhost:5000/api/chat
```

Body:

```json
{
  "message": "Explain Angular"
}
```

---

## 10. Angular gọi backend

### Service

```ts
chat(message: string) {
  return this.http.post<any>('http://localhost:5000/api/chat', {
    message
  });
}
```

---

### Component

```ts
this.chatService.chat(input).subscribe(res => {
  const text =
    res.candidates[0].content.parts[0].text;

  console.log(text);
});
```

---

## 11. Streaming (nâng cao - optional)

Vertex AI hỗ trợ stream nhưng .NET cần xử lý kiểu:

```csharp
var response = await _httpClient.SendAsync(request,
    HttpCompletionOption.ResponseHeadersRead);

using var stream = await response.Content.ReadAsStreamAsync();
using var reader = new StreamReader(stream);

while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync();
    Console.WriteLine(line);
}
```

---

## 12. Deploy

### Cách đơn giản:

```bash
dotnet publish -c Release
dotnet AiBackend.dll
```

---

### Hoặc Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish .
ENTRYPOINT ["dotnet", "AiBackend.dll"]
```

---

## 13. Checklist

* [ ] Đúng PROJECT_ID
* [ ] Đúng model name
* [ ] Set GOOGLE_APPLICATION_CREDENTIALS
* [ ] API trả về có `candidates`
* [ ] Angular parse đúng response

---

**Xong.**
