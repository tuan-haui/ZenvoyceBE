namespace Zenvoyce.Application.Abstractions.Services;

/// <summary>
/// Render HTML+CSS thành PDF (A4) bằng headless browser.
/// </summary>
public interface IInvoicePdfRenderer
{
    Task<byte[]> RenderPdfAsync(string html, string? css, CancellationToken cancellationToken);
}
