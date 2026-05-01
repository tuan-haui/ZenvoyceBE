using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Invoices.Commands.CancelInvoice;

public record CancelInvoiceCommand(Guid InvoiceId, string LyDo) : IRequest<bool>;

public class CancelInvoiceCommandValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.LyDo).NotEmpty().WithMessage("Vui lòng nhập lý do hủy hóa đơn.");
    }
}

public class CancelInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : IRequestHandler<CancelInvoiceCommand, bool>
{
    private const string CancelledStatus = "Cancelled";
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Signed", "Issued"
    };

    public async Task<bool> Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

        if (!AllowedStatuses.Contains(invoice.Trangthai))
        {
            throw new InvalidOperationException(
                $"Chỉ hủy được hóa đơn ở trạng thái Signed hoặc Issued. Trạng thái hiện tại: {invoice.Trangthai}");
        }

        var now = dateTimeProvider.UtcNow;

        var history = new HoadonLichsu
        {
            Id = Guid.NewGuid(),
            Hoadonid = request.InvoiceId,
            Hanhdong = $"Hủy hóa đơn. Lý do: {request.LyDo}",
            Trangthaicu = invoice.Trangthai,
            Trangthaimoi = CancelledStatus,
            Thoigian = now,
            Nguoidungid = currentUserService.UserId
        };

        await invoiceRepository.UpdateStatusAsync(
            request.InvoiceId,
            CancelledStatus,
            history,
            now,
            currentUserService.UserId,
            cancellationToken);

        return true;
    }
}
