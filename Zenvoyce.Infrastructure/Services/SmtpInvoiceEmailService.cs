using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Infrastructure.Options;

namespace Zenvoyce.Infrastructure.Services;

public sealed class SmtpInvoiceEmailService(
    IOptions<SmtpSettings> smtpOptions,
    ILogger<SmtpInvoiceEmailService> logger) : IInvoiceEmailService
{
    private readonly SmtpSettings _smtp = smtpOptions.Value;

    public async Task SendInvoicePdfAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[] pdfBytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new InvalidOperationException("Email người nhận không hợp lệ.");
        }

        if (pdfBytes.Length == 0)
        {
            throw new InvalidOperationException("File PDF đính kèm đang rỗng.");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_smtp.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        bodyBuilder.Attachments.Add(fileName, pdfBytes, ContentType.Parse("application/pdf"));
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureSocketOption = _smtp.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_smtp.Host, _smtp.Port, secureSocketOption, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_smtp.Username))
            {
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gửi email hóa đơn thất bại tới {ToEmail}.", toEmail);
            throw new InvalidOperationException("Không gửi được email hóa đơn. Vui lòng thử lại sau.", ex);
        }
    }
}
