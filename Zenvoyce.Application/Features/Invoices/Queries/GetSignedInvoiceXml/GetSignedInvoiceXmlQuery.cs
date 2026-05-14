using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Features.Invoices.Queries.GetSignedInvoiceXml;

public record GetSignedInvoiceXmlQuery(
    Guid? Id,
    string? SoHoadon,
    string? KyHieu,
    string? MaSoThue,
    DateTime? NgayLap) : IRequest<SignedInvoiceXmlResult>;

public sealed record SignedInvoiceXmlResult(string XmlContent, string Filename);

public class GetSignedInvoiceXmlQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetSignedInvoiceXmlQuery, SignedInvoiceXmlResult>
{
    public async Task<SignedInvoiceXmlResult> Handle(GetSignedInvoiceXmlQuery request, CancellationToken cancellationToken)
    {
        if (!request.Id.HasValue && string.IsNullOrWhiteSpace(request.SoHoadon))
        {
            throw new ArgumentException("Vui lòng cung cấp id hoá đơn hoặc số hoá đơn.");
        }

        IReadOnlyList<Hoadon> invoices = await invoiceRepository.FindInvoicesForSignedXmlAsync(
            request.Id,
            request.SoHoadon,
            request.KyHieu,
            request.MaSoThue,
            request.NgayLap,
            cancellationToken);

        if (invoices.Count == 0)
        {
            throw new KeyNotFoundException("Không tìm thấy hoá đơn.");
        }

        if (invoices.Count > 1)
        {
            throw new InvalidOperationException(
                "Tìm thấy nhiều hoá đơn phù hợp. Vui lòng bổ sung ký hiệu, mã số thuế khách hàng hoặc ngày lập hoá đơn.");
        }

        var invoice = invoices[0];
        if (string.IsNullOrWhiteSpace(invoice.Xmldaky))
        {
            throw new InvalidOperationException("Hoá đơn này chưa được ký số.");
        }

        var baseName = !string.IsNullOrWhiteSpace(invoice.Sohoadon)
            ? invoice.Sohoadon!.Trim()
            : invoice.Id.ToString("N");
        var filename = $"hoadon-{baseName}.xml";

        return new SignedInvoiceXmlResult(invoice.Xmldaky, filename);
    }
}
