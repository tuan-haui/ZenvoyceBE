# AI Chat Streaming Integration Guide

## Tổng quan

Backend cung cấp 2 endpoint cho AI Chat:

1. **Non-stream** (POST): Chờ full response từ AI model
2. **Stream** (GET): Nhận response real-time dưới dạng Server-Sent Events (SSE)

---

## Architecture

```
Frontend (Angular/React)
    ↓ (HTTP GET - EventSource hoặc fetch)
ASP.NET Core Controller (AiController)
    ↓ (IAsyncEnumerable<string>)
VertexAiService
    ↓ (HttpCompletionOption.ResponseHeadersRead)
Vertex AI API (?alt=sse)
```

---

## 1. Non-Stream Endpoint (Đã xong)

### Request
```http
POST /api/ai/chat
Content-Type: application/json

{
  "message": "Xin chào, bạn là ai?"
}
```

### Response
```json
{
  "data": {
    "text": "Tôi là Claude...",
    "model": "gemini-2.5-flash",
    "finishReason": "STOP",
    "usage": {
      "promptTokens": 10,
      "completionTokens": 50,
      "totalTokens": 60
    }
  },
  "isSuccess": true,
  "message": ""
}
```

---

## 2. Stream Endpoint (SSE) - **PRODUCTION READY**

### URL
```
GET /api/ai/chat-stream?message={message}
```

### Request Headers (Auto)
- `Content-Type: text/event-stream`
- `Cache-Control: no-cache`
- `Connection: keep-alive`
- `X-Accel-Buffering: no`

### Response Format (Server-Sent Events)
```
data: {"text":"Xin"}
data: {"text":" chào,"}
data: {"text":" bạn"}
data: {"text":" là"}
data: {"text":" ai?"}
data: [DONE]
```

---

## 3. Frontend Integration Examples

### JavaScript - EventSource (Recommended)

```javascript
// Simple EventSource approach
function streamAiChat(message) {
  const eventSource = new EventSource(`/api/ai/chat-stream?message=${encodeURIComponent(message)}`);
  let fullText = '';

  eventSource.addEventListener('message', (event) => {
    if (event.data === '[DONE]') {
      console.log('Stream kết thúc');
      eventSource.close();
      return;
    }

    try {
      const chunk = JSON.parse(event.data);
      fullText += chunk.text;
      console.log('Chunk:', chunk.text);
      // Update UI here
    } catch (e) {
      console.error('Parse error:', e);
    }
  });

  eventSource.onerror = (event) => {
    console.error('EventSource error:', event);
    eventSource.close();
  };
}

streamAiChat('Hello, who are you?');
```

### TypeScript - Fetch API (Better Control)

```typescript
async function streamAiChatFetch(message: string) {
  const url = `/api/ai/chat-stream?message=${encodeURIComponent(message)}`;
  
  try {
    const response = await fetch(url);
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    const reader = response.body?.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    if (!reader) throw new Error('No reader');

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      
      // Process lines
      const lines = buffer.split('\n');
      buffer = lines.pop() || ''; // Keep incomplete line

      for (const line of lines) {
        if (!line.startsWith('data: ')) continue;

        const dataStr = line.slice(6);
        if (dataStr === '[DONE]') {
          console.log('Stream finished');
          return;
        }

        try {
          const chunk = JSON.parse(dataStr);
          console.log('Chunk:', chunk.text);
          // Update UI here
        } catch (e) {
          console.error('Parse error:', e);
        }
      }
    }
  } catch (error) {
    console.error('Stream error:', error);
  }
}

streamAiChatFetch('Hello, who are you?');
```

### Angular - HttpClient

```typescript
// Service
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class AiChatService {
  constructor(private http: HttpClient) {}

  streamChat(message: string) {
    const url = `/api/ai/chat-stream?message=${encodeURIComponent(message)}`;
    return this.http.get(url, {
      responseType: 'text',
      reportProgress: true,
      observe: 'events'
    });
  }
}

// Component
import { HttpEvent, HttpResponse } from '@angular/common/http';
import { Component } from '@angular/core';

@Component({
  selector: 'app-ai-chat',
  templateUrl: './ai-chat.component.html'
})
export class AiChatComponent {
  responseText = '';

  constructor(private aiService: AiChatService) {}

  onChat(message: string) {
    this.aiService.streamChat(message).subscribe({
      next: (event: HttpEvent<any>) => {
        if (event instanceof HttpResponse) {
          console.log('Complete response:', event.body);
        }
        // Handle progress if needed
      },
      error: (err) => console.error('Stream error:', err),
      complete: () => console.log('Stream complete')
    });
  }
}
```

**Better approach - Parse SSE in Angular:**

```typescript
// Service
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AiChatService {
  constructor(private http: HttpClient) {}

  streamChat(message: string): Observable<string> {
    return new Observable((observer) => {
      const url = `/api/ai/chat-stream?message=${encodeURIComponent(message)}`;
      
      fetch(url)
        .then((response) => {
          if (!response.ok) throw new Error(`HTTP ${response.status}`);
          return response.body?.getReader();
        })
        .then((reader) => {
          if (!reader) throw new Error('No reader');
          
          const decoder = new TextDecoder();
          let buffer = '';

          const processChunk = async () => {
            while (true) {
              const { done, value } = await reader.read();
              if (done) {
                observer.complete();
                break;
              }

              buffer += decoder.decode(value, { stream: true });
              const lines = buffer.split('\n');
              buffer = lines.pop() || '';

              for (const line of lines) {
                if (line.startsWith('data: ')) {
                  const dataStr = line.slice(6);
                  if (dataStr === '[DONE]') {
                    observer.complete();
                    return;
                  }
                  try {
                    const chunk = JSON.parse(dataStr);
                    observer.next(chunk.text);
                  } catch (e) {
                    console.error('Parse error:', e);
                  }
                }
              }
            }
          };

          processChunk().catch((error) => observer.error(error));
        })
        .catch((error) => observer.error(error));
    });
  }
}

// Component
@Component({
  selector: 'app-ai-chat',
  templateUrl: './ai-chat.component.html'
})
export class AiChatComponent {
  responseText = '';
  isLoading = false;

  constructor(private aiService: AiChatService) {}

  onChat(message: string) {
    this.responseText = '';
    this.isLoading = true;

    this.aiService.streamChat(message).subscribe({
      next: (chunk: string) => {
        this.responseText += chunk;
      },
      error: (err) => {
        console.error('Stream error:', err);
        this.isLoading = false;
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }
}
```

---

## 4. React Example

```typescript
import { useState } from 'react';

export function AiChat() {
  const [response, setResponse] = useState('');
  const [loading, setLoading] = useState(false);

  const handleStreamChat = async (message: string) => {
    setResponse('');
    setLoading(true);

    try {
      const res = await fetch(
        `/api/ai/chat-stream?message=${encodeURIComponent(message)}`
      );

      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const reader = res.body?.getReader();
      if (!reader) throw new Error('No reader');

      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const dataStr = line.slice(6);
            if (dataStr === '[DONE]') continue;

            try {
              const chunk = JSON.parse(dataStr);
              setResponse((prev) => prev + chunk.text);
            } catch (e) {
              console.error('Parse error:', e);
            }
          }
        }
      }
    } catch (error) {
      console.error('Stream error:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <button onClick={() => handleStreamChat('Hello!')}>
        Start Chat
      </button>
      <div>{response}</div>
      {loading && <div>Loading...</div>}
    </div>
  );
}
```

---

## 5. Error Handling

### Server-Side Error Response
```
data: {"error": "Message is required"}

hoặc

data: {"error": "Vertex AI request failed with status 400: ..."}
```

### Frontend Error Handling
```javascript
eventSource.onerror = (event) => {
  console.error('EventSource error:', event);
  console.error('Ready state:', eventSource.readyState);
  // 0 = CONNECTING, 1 = OPEN, 2 = CLOSED
  eventSource.close();
};
```

---

## 6. CORS Configuration

Backend đã cấu hình CORS cho các origins:
- `http://localhost:4200`
- `https://localhost:4200`
- `https://zenvoyce-fe.vercel.app`
- `https://feephim.com`

Frontend có thể gọi trực tiếp bằng relative URL hoặc CORS request.

---

## 7. Authentication

- Endpoint `/api/ai/chat` yêu cầu JWT token (authorization header)
- Endpoint `/api/ai/chat-stream` đánh dấu `[AllowAnonymous]` nhưng có thể thêm auth nếu cần

Nếu cần bảo vệ streaming endpoint, thêm JWT token vào URL query:
```
GET /api/ai/chat-stream?message=Hello&token=JWT_TOKEN
```

---

## 8. Performance Tips

1. **Buffer management**: Không buffer quá nhiều chunk, cập nhật UI ngay
2. **Connection timeout**: Đặt timeout 30-60s để tránh hanging connection
3. **Cancellation**: Implement cancellation token để user có thể dừng streaming
4. **Memory**: Xóa old EventSource listeners để tránh memory leak

---

## 9. Testing

### curl
```bash
curl -X GET "http://localhost:5000/api/ai/chat-stream?message=Hello%20world"
```

### Postman
1. Chọn GET method
2. URL: `http://localhost:5000/api/ai/chat-stream?message=Hello`
3. Response type chọn **"Stream"**

---

## 10. Deployment Checklist

- [ ] ✅ Service: `ChatStreamAsync()` implement đầy đủ
- [ ] ✅ Controller: `StreamChat()` endpoint với SSE headers
- [ ] ✅ DI: HttpClient registered, VertexAiService configured
- [ ] ✅ Config: VertexAI ProjectId, Model, Location trong appsettings.json
- [ ] ✅ CORS: Origins configured
- [ ] ✅ Nginx (if behind proxy): `X-Accel-Buffering: no` header set
- [ ] ✅ Frontend: SSE consumer code ready
- [ ] Frontend: Error handling & connection timeout

---

## Troubleshooting

| Vấn đề | Nguyên nhân | Giải pháp |
|--------|-----------|----------|
| Vẫn chờ hết rồi mới nhận data | Thiếu `?alt=sse` hoặc `ResponseHeadersRead` | Đã fix trong code |
| Client nhận chunk nhưng chậm | Thiếu `FlushAsync()` | Đã thêm trong controller |
| Nginx gộp chunks | Buffer proxy | Thêm `X-Accel-Buffering: no` |
| EventSource CORS error | CORS chưa config | Kiểm tra appsettings.json |
| Stream timeout giữa chừng | Token hết hạn | Cache access token 1 giờ |

---

## API Endpoint Summary

| Endpoint | Method | Description | Auth |
|----------|--------|-------------|------|
| `/api/ai/chat` | POST | Non-stream, full response | Optional |
| `/api/ai/chat-stream` | GET | Stream real-time SSE | Optional |

---

**Last Updated:** 2026-05-14
**Status:** ✅ Production Ready
