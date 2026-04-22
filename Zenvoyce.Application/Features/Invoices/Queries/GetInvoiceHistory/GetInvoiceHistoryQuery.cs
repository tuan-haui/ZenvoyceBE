using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Application.Features.Invoices.Queries.GetInvoiceHistory;

public record GetInvoiceHistoryQuery(Guid InvoiceId) : IRequest<IReadOnlyCollection<InvoiceHistoryItemDto>>;

public class GetInvoiceHistoryQueryValidator : AbstractValidator<GetInvoiceHistoryQuery>
{
    public GetInvoiceHistoryQueryValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}

public class GetInvoiceHistoryQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetInvoiceHistoryQuery, IReadOnlyCollection<InvoiceHistoryItemDto>>
{
    public async Task<IReadOnlyCollection<InvoiceHistoryItemDto>> Handle(GetInvoiceHistoryQuery request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new KeyNotFoundException("Không tìm thấy hóa đơn.");
        }

        var history = await invoiceRepository.GetInvoiceHistoryAsync(request.InvoiceId, cancellationToken);
        return history.Select(x => new InvoiceHistoryItemDto
        {
            Id = x.Id,
            HoadonId = x.Hoadonid,
            Hanhdong = x.Hanhdong,
            TrangthaiCu = x.Trangthaicu,
            TrangthaiMoi = x.Trangthaimoi,
            Thoigian = x.Thoigian,
            NguoidungId = x.Nguoidungid
        }).ToArray();
    }
}
