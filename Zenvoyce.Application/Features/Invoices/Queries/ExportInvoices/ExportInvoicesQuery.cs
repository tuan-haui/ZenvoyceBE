using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Application.Features.Invoices.Queries.ExportInvoices;

public record ExportInvoicesQuery(
    Guid? KhachhangId,
    string? Trangthai,
    DateTime? TuNgay,
    DateTime? DenNgay) : IRequest<ExportResultDto>;

public class ExportInvoicesQueryValidator : AbstractValidator<ExportInvoicesQuery>
{
    public ExportInvoicesQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.TuNgay.HasValue || !x.DenNgay.HasValue || x.TuNgay <= x.DenNgay)
            .WithMessage("Khoảng thời gian không hợp lệ.");
    }
}

public class ExportInvoicesQueryHandler(
    IInvoiceRepository invoiceRepository,
    IInvoiceExportService exportService)
    : IRequestHandler<ExportInvoicesQuery, ExportResultDto>
{
    public async Task<ExportResultDto> Handle(ExportInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await invoiceRepository.GetInvoicesWithLineItemsAsync(
            request.KhachhangId,
            request.Trangthai,
            request.TuNgay,
            request.DenNgay,
            cancellationToken);

        var result = await exportService.GenerateInvoiceListExcelAsync(invoices, cancellationToken);
        return result;
    }
}
