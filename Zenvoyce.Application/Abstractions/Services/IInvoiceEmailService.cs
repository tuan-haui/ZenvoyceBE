namespace Zenvoyce.Application.Abstractions.Services;

public interface IInvoiceEmailService
{
    Task SendInvoicePdfAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[] pdfBytes,
        string fileName,
        CancellationToken cancellationToken);
}
