using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Application.Features.Invoices.Queries.ExportSalesReport;

public record ExportSalesReportQuery(
    Guid? DonviId,
    Guid? KhachhangId,
    DateTime? TuNgay,
    DateTime? DenNgay) : IRequest<ExportResultDto>;

public class ExportSalesReportQueryValidator : AbstractValidator<ExportSalesReportQuery>
{
    public ExportSalesReportQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.TuNgay.HasValue || !x.DenNgay.HasValue || x.TuNgay <= x.DenNgay)
            .WithMessage("Khoảng thời gian không hợp lệ.");
    }
}

public class ExportSalesReportQueryHandler(
    IInvoiceRepository invoiceRepository,
    IInvoiceExportService exportService)
    : IRequestHandler<ExportSalesReportQuery, ExportResultDto>
{
    public async Task<ExportResultDto> Handle(ExportSalesReportQuery request, CancellationToken cancellationToken)
    {
        var salesData = await invoiceRepository.GetSalesByCustomerAsync(
            request.DonviId,
            request.KhachhangId,
            request.TuNgay,
            request.DenNgay,
            cancellationToken);

        var result = await exportService.GenerateSalesReportExcelAsync(salesData, cancellationToken);
        return result;
    }
}
