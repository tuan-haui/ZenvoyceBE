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
        var invoices = await invoiceRepository.GetInvoicesAsync(
            request.KhachhangId,
            request.Trangthai,
            request.TuNgay,
            request.DenNgay,
            cancellationToken);

        return invoices.Select(x => new InvoiceListItemDto
        {
            Id = x.Id,
            DonviId = x.Donviid,
            KhachhangId = x.Khachhangid,
            MauctyId = x.Mauctyid,
            Kyhieu = x.Kyhieu,
            Sohoadon = x.Sohoadon,
            Ngaylap = x.Ngaylap,
            Tongtien = x.Tongtien,
            Tienthue = x.Tienthue,
            Tongthanhtoan = x.Tongthanhtoan,
            Trangthai = x.Trangthai
        }).ToArray();
    }
}
