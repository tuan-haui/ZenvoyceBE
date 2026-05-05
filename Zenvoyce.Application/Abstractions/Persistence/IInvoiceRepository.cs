using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;
public interface IInvoiceRepository
{
    Task CreateDraftInvoiceAsync(Hoadon invoice, IReadOnlyCollection<HoadonHanghoa> items, HoadonLichsu history, CancellationToken cancellationToken);
    Task<Hoadon?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateStatusAsync(Guid invoiceId, string newStatus, HoadonLichsu history, DateTime updatedAt, Guid? updatedBy, CancellationToken cancellationToken);
    Task UpdateSignedAsync(Guid invoiceId, string xmlDaky, HoadonLichsu history, DateTime updatedAt, Guid? updatedBy, CancellationToken cancellationToken);
    Task UpdatePublishedAsync(Guid invoiceId, string soHoadon, HoadonLichsu history, DateTime updatedAt, Guid? updatedBy, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Hoadon>> GetInvoicesAsync(Guid? khachhangId, string? trangthai, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<HoadonLichsu>> GetInvoiceHistoryAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SalesReportRow>> GetSalesByCustomerAsync(
        Guid? donviId,
        Guid? khachhangId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HoadonHanghoa>> GetInvoiceLinesAsync(Guid invoiceId, CancellationToken cancellationToken);
}
