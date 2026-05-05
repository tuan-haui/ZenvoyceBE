# Hướng dẫn cấu hình SMTP trong Backend .NET 9 để gửi file PDF

## 1. Giới thiệu

Tài liệu này hướng dẫn lập trình viên cấu hình SMTP trong ứng dụng backend .NET 9 để gửi email kèm file PDF cho khách hàng.

---

## 2. Chuẩn bị

* .NET 9 SDK
* Tài khoản SMTP (Gmail, Outlook, SendGrid, v.v.)
* File PDF cần gửi

---

## 3. Cài đặt thư viện cần thiết

Sử dụng thư viện `System.Net.Mail` (có sẵn) hoặc `MailKit` (khuyến nghị).

Cài MailKit:

```bash
dotnet add package MailKit
```

---

## 4. Cấu hình SMTP trong appsettings.json

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password",
  "EnableSsl": true,
  "From": "your-email@gmail.com"
}
```

---

## 5. Tạo Model cấu hình

```csharp
public class SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool EnableSsl { get; set; }
    public string From { get; set; }
}
```

---

## 6. Đăng ký cấu hình trong Program.cs

```csharp
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));
```

---

## 7. Tạo Service gửi email

### Dùng MailKit

```csharp
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;

public class EmailService
{
    private readonly SmtpSettings _smtp;

    public EmailService(IOptions<SmtpSettings> smtp)
    {
        _smtp = smtp.Value;
    }

    public async Task SendEmailWithPdfAsync(string toEmail, string subject, string body, byte[] pdfBytes)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_smtp.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };

        builder.Attachments.Add("document.pdf", pdfBytes, new ContentType("application", "pdf"));

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_smtp.Host, _smtp.Port, _smtp.EnableSsl);
        await smtp.AuthenticateAsync(_smtp.Username, _smtp.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
```

---

## 8. Gọi Service từ Controller

```csharp
[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;

    public EmailController(EmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-pdf")]
    public async Task<IActionResult> SendPdf()
    {
        var pdfBytes = System.IO.File.ReadAllBytes("sample.pdf");

        await _emailService.SendEmailWithPdfAsync(
            "customer@example.com",
            "Your PDF Document",
            "<h1>Xin chào!</h1><p>Đây là file PDF của bạn.</p>",
            pdfBytes
        );

        return Ok("Email sent successfully");
    }
}
```

---

## 9. Lưu ý bảo mật

* Không hardcode mật khẩu trong code
* Sử dụng Secret Manager hoặc Environment Variables
* Với Gmail cần dùng App Password thay vì password thường

---

## 10. Debug & xử lý lỗi

* Kiểm tra firewall và port SMTP
* Kiểm tra SSL/TLS
* Log exception chi tiết

---

## 11. Kết luận

Bạn đã cấu hình thành công SMTP trong .NET 9 để gửi email kèm file PDF. Có thể mở rộng để gửi nhiều file hoặc template email.
