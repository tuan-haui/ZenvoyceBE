using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Application.Features.Invoices.Queries.GetInvoices;

public record GetInvoicesQuery(
    Guid? KhachhangId,
    string? Trangthai,
    DateTime? TuNgay,
    DateTime? DenNgay) : IRequest<IReadOnlyCollection<InvoiceListItemDto>>;

public class GetInvoicesQueryValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.TuNgay.HasValue || !x.DenNgay.HasValue || x.TuNgay <= x.DenNgay)
            .WithMessage("Khoảng thời gian không hợp lệ.");
    }
}

public class GetInvoicesQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetInvoicesQuery, IReadOnlyCollection<InvoiceListItemDto>>
{
    public async Task<IReadOnlyCollection<InvoiceListItemDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        return await invoiceRepository.GetInvoicesAsync(
            request.KhachhangId,
            request.Trangthai,
            request.TuNgay,
            request.DenNgay,
            cancellationToken);
    }
}
