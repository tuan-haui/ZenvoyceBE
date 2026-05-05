using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Invoices.Services;

namespace Zenvoyce.Application.Features.Invoices.Commands.SendInvoiceEmail;

public record SendInvoiceEmailCommand(Guid InvoiceId) : IRequest<SendInvoiceEmailResultDto>;

public class SendInvoiceEmailResultDto
{
    public bool Sent { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class SendInvoiceEmailCommandHandler(
    IInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    ITemplateRepository templateRepository,
    ITemplateRenderer templateRenderer,
    IInvoicePdfRenderer pdfRenderer,
    IInvoiceEmailService invoiceEmailService) : IRequestHandler<SendInvoiceEmailCommand, SendInvoiceEmailResultDto>
{
    public async Task<SendInvoiceEmailResultDto> Handle(SendInvoiceEmailCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new KeyNotFoundException("Không tìm thấy hóa đơn.");
        }

        if (!string.Equals(invoice.Trangthai, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ gửi email khi hóa đơn đã phát hành (Issued).");
        }

        var customer = await customerRepository.GetByIdAsync(invoice.Khachhangid, cancellationToken);
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
        {
            throw new InvalidOperationException("Khách hàng không có email nhận hóa đơn.");
        }

        if (string.IsNullOrWhiteSpace(invoice.XmlMetadata))
        {
            throw new InvalidOperationException("Hoá đơn chưa có XML metadata để render.");
        }

        if (invoice.Mauctyid == Guid.Empty)
        {
            throw new InvalidOperationException("Hoá đơn chưa gắn mẫu công ty.");
        }

        var companyTemplate = await templateRepository.GetCompanyTemplateByIdAsync(invoice.Mauctyid, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu công ty của hoá đơn.");

        var baseTemplate = await templateRepository.GetBaseTemplateByIdAsync(companyTemplate.Maugocid, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu hoá đơn gốc.");

        if (string.IsNullOrWhiteSpace(baseTemplate.HtmlContent))
        {
            throw new InvalidOperationException("Mẫu hoá đơn gốc chưa có HTML content.");
        }

        var renderXml = string.IsNullOrWhiteSpace(invoice.Xmldaky)
            ? invoice.XmlMetadata
            : invoice.Xmldaky;
        var context = InvoiceXmlContextMapper.Map(renderXml!);
        var renderedHtml = templateRenderer.Render(baseTemplate.HtmlContent, context);
        var combinedCss = JoinCss(baseTemplate.CssContent, companyTemplate.Css);
        var pdfBytes = await pdfRenderer.RenderPdfAsync(renderedHtml, combinedCss, cancellationToken);

        var invoiceCode = string.IsNullOrWhiteSpace(invoice.Sohoadon)
            ? invoice.Id.ToString()
            : invoice.Sohoadon.Trim();
        var subject = $"[Zenvoyce] Hoa don dien tu {invoiceCode}";
        var htmlBody = BuildEmailBody(customer.Tenkhachhang, invoiceCode);
        var fileName = $"hoa-don-{invoiceCode}.pdf";

        await invoiceEmailService.SendInvoicePdfAsync(
            customer.Email.Trim(),
            subject,
            htmlBody,
            pdfBytes,
            fileName,
            cancellationToken);

        return new SendInvoiceEmailResultDto
        {
            Sent = true,
            Message = $"Đã gửi hóa đơn tới {customer.Email.Trim()}."
        };
    }

    private static string BuildEmailBody(string? customerName, string invoiceCode)
    {
        var safeName = string.IsNullOrWhiteSpace(customerName) ? "Quy khach" : customerName.Trim();
        return $"""
                <p>Xin chao {safeName},</p>
                <p>Hoa don dien tu <strong>{invoiceCode}</strong> duoc dinh kem trong email nay.</p>
                <p>Tran trong,<br/>Zenvoyce</p>
                """;
    }

    private static string? JoinCss(string? baseCss, string? companyCss)
    {
        if (string.IsNullOrWhiteSpace(baseCss) && string.IsNullOrWhiteSpace(companyCss))
        {
            return null;
        }

        return $"{baseCss}\n{companyCss}";
    }
}
