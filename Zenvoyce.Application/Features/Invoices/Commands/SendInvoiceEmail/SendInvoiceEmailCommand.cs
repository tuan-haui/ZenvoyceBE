using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;

namespace Zenvoyce.Application.Features.Invoices.Commands.SendInvoiceEmail;

public record SendInvoiceEmailCommand(Guid InvoiceId) : IRequest<SendInvoiceEmailResultDto>;

public class SendInvoiceEmailResultDto
{
    public bool Sent { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class SendInvoiceEmailCommandHandler(
    IInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository) : IRequestHandler<SendInvoiceEmailCommand, SendInvoiceEmailResultDto>
{
    public async Task<SendInvoiceEmailResultDto> Handle(SendInvoiceEmailCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new KeyNotFoundException("Không tìm thấy hóa đơn.");
        }

        if (!string.Equals(invoice.Trangthai, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ gửi email khi hóa đơn đã phát hành (Issued).");
        }

        var customer = await customerRepository.GetByIdAsync(invoice.Khachhangid, cancellationToken);
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
        {
            throw new InvalidOperationException("Khách hàng không có email nhận hóa đơn.");
        }

        await Task.Delay(500, cancellationToken);

        return new SendInvoiceEmailResultDto
        {
            Sent = true,
            Message = $"Đã gửi (mock) hóa đơn tới {customer.Email.Trim()}."
        };
    }
}
