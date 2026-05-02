using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Application.Features.Invoices.Queries.GetSalesReport;

public record GetSalesReportQuery(
    Guid? DonviId,
    Guid? KhachhangId,
    DateTime? TuNgay,
    DateTime? DenNgay) : IRequest<IReadOnlyCollection<SalesReportRow>>;

public class GetSalesReportQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetSalesReportQuery, IReadOnlyCollection<SalesReportRow>>
{
    public Task<IReadOnlyCollection<SalesReportRow>> Handle(
        GetSalesReportQuery request,
        CancellationToken cancellationToken) =>
        invoiceRepository.GetSalesByCustomerAsync(
            request.DonviId,
            request.KhachhangId,
            request.TuNgay,
            request.DenNgay,
            cancellationToken);
}
