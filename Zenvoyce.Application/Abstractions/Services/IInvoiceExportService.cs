using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Application.Abstractions.Services;

public interface IInvoiceExportService
{
    Task<ExportResultDto> GenerateInvoiceListExcelAsync(
        IReadOnlyCollection<InvoiceForExportDto> invoices,
        CancellationToken cancellationToken);

    Task<ExportResultDto> GenerateSalesReportExcelAsync(
        IReadOnlyCollection<SalesReportRow> salesData,
        CancellationToken cancellationToken);
}
