using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Application.Features.Invoices.Services;

namespace Zenvoyce.Application.Features.Invoices.Queries.PreviewInvoicePdf;

public record PreviewInvoicePdfQuery(Guid InvoiceId) : IRequest<InvoicePreviewResultDto>;

public class PreviewInvoicePdfQueryHandler(
    IInvoiceRepository invoiceRepository,
    ITemplateRepository templateRepository,
    ITemplateRenderer templateRenderer,
    IInvoicePdfRenderer pdfRenderer)
    : IRequestHandler<PreviewInvoicePdfQuery, InvoicePreviewResultDto>
{
    public async Task<InvoicePreviewResultDto> Handle(PreviewInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hoá đơn.");

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

        var filename = string.IsNullOrWhiteSpace(invoice.Sohoadon)
            ? $"hoadon-{invoice.Id}.pdf"
            : $"hoadon-{invoice.Sohoadon}.pdf";

        return new InvoicePreviewResultDto
        {
            PdfBytes = pdfBytes,
            Filename = filename,
            ContentType = "application/pdf"
        };
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
