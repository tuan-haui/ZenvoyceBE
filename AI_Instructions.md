# ⚡ Những thông tin bắt buộc cần lấy từ Google Cloud để chạy AI (Vertex AI + Claude)

Tài liệu này chỉ tập trung vào: **bạn cần lấy gì để code chạy được**.

---

## 1. PROJECT_ID

* Lấy tại: Google Cloud Console → Dashboard

Ví dụ:

```
my-ai-project-123
```

👉 Dùng trong code:

```
projects/{PROJECT_ID}
```

---

## 2. LOCATION (REGION)

* Thường dùng nhất:

```
us-central1
```

👉 Quan trọng:

* Phải trùng với region của model trong Vertex AI

---

## 3. MODEL NAME

* Lấy tại: Vertex AI → Model Garden

Ví dụ đúng:

```
claude-3-5-sonnet@20240620
```

⚠️ Lỗi phổ biến:

* Dùng sai tên model → API fail ngay

---

## 4. SERVICE ACCOUNT (CREDENTIAL)

Bạn cần:

* Tạo Service Account
* Gán quyền:

  * Vertex AI User

Sau đó:

* Tải file JSON

---

## 5. GOOGLE_APPLICATION_CREDENTIALS

Set biến môi trường:

```bash
set GOOGLE_APPLICATION_CREDENTIALS=path\\to\\service-account.json
```

👉 Đây là thứ giúp backend authenticate với Google

---

## 6. API ENDPOINT

Format cố định:

```text
https://{location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{location}/publishers/anthropic/models/{model}:generateContent
```

Ví dụ thực tế:

```text
https://us-central1-aiplatform.googleapis.com/v1/projects/my-ai-project-123/locations/us-central1/publishers/anthropic/models/claude-3-5-sonnet@20240620:generateContent
```

---

## 7. ACCESS TOKEN (tự generate từ credential)

Không cần lấy thủ công.

Trong .NET:

* Dùng GoogleCredential để tự generate

---

## 8. Tóm tắt (cực ngắn)

Bạn chỉ cần 5 thứ:

* PROJECT_ID
* LOCATION
* MODEL_NAME
* SERVICE_ACCOUNT_JSON
* API_ENDPOINT

👉 Có đủ 5 cái này → code chạy được ngay

---

## 9. Mapping vào code của bạn

```csharp
var projectId = "YOUR_PROJECT_ID";
var location = "us-central1";
var model = "claude-3-5-sonnet@20240620";
```

---

**Xong.**
