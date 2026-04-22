using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Invoices.Commands.ForwardInvoice;

public record ForwardInvoiceCommand(Guid InvoiceId) : IRequest<bool>;

public class ForwardInvoiceCommandValidator : AbstractValidator<ForwardInvoiceCommand>
{
    public ForwardInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}

public class ForwardInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : IRequestHandler<ForwardInvoiceCommand, bool>
{
    private const string DraftStatus = "Draft";
    private const string PendingSignStatus = "PendingSign";

    public async Task<bool> Handle(ForwardInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

        if (!string.Equals(invoice.Trangthai, DraftStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ hóa đơn ở trạng thái Draft mới được gửi chờ ký.");
        }

        var history = new HoadonLichsu
        {
            Id = Guid.NewGuid(),
            Hoadonid = request.InvoiceId,
            Hanhdong = "Gửi hóa đơn chờ ký",
            Trangthaicu = DraftStatus,
            Trangthaimoi = PendingSignStatus,
            Thoigian = dateTimeProvider.UtcNow,
            Nguoidungid = currentUserService.UserId
        };

        await invoiceRepository.UpdateStatusAsync(
            request.InvoiceId,
            PendingSignStatus,
            history,
            dateTimeProvider.UtcNow,
            currentUserService.UserId,
            cancellationToken);

        return true;
    }
}
