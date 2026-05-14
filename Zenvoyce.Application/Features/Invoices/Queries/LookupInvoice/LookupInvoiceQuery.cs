using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Application.Features.Invoices.Queries.LookupInvoice;

public record LookupInvoiceQuery(string? Sohoadon, string? Masothue) : IRequest<IReadOnlyCollection<InvoiceListItemDto>>;

public class LookupInvoiceQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<LookupInvoiceQuery, IReadOnlyCollection<InvoiceListItemDto>>
{
    public async Task<IReadOnlyCollection<InvoiceListItemDto>> Handle(LookupInvoiceQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sohoadon) && string.IsNullOrWhiteSpace(request.Masothue))
        {
            return Array.Empty<InvoiceListItemDto>();
        }

        return await invoiceRepository.LookupInvoicesAsync(request.Sohoadon, request.Masothue, cancellationToken);
    }
}
