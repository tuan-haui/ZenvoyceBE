using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Invoices.Commands.PublishInvoice;

public record PublishInvoiceCommand(Guid InvoiceId) : IRequest<PublishInvoiceResultDto>;

public class PublishInvoiceResultDto
{
    public Guid Id { get; set; }
    public string Trangthai { get; set; } = string.Empty;
    public string SoHoadon { get; set; } = string.Empty;
}

public class PublishInvoiceCommandValidator : AbstractValidator<PublishInvoiceCommand>
{
    public PublishInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}

public class PublishInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : IRequestHandler<PublishInvoiceCommand, PublishInvoiceResultDto>
{
    private const string IssuedStatus = "Issued";

    public async Task<PublishInvoiceResultDto> Handle(PublishInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

        if (!string.Equals(invoice.Trangthai, "Signed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ hóa đơn đã ký số mới có thể phát hành. Trạng thái hiện tại: " + invoice.Trangthai);
        }

        if (string.IsNullOrWhiteSpace(invoice.Xmldaky))
        {
            throw new InvalidOperationException("Hóa đơn chưa có dữ liệu XML ký số.");
        }

        var now = dateTimeProvider.UtcNow;

        var soHoadon = invoice.Sohoadon ?? GenerateSoHoadon(now);

        var history = new HoadonLichsu
        {
            Id = Guid.NewGuid(),
            Hoadonid = request.InvoiceId,
            Hanhdong = "Phát hành hóa đơn (Gửi Thuế)",
            Trangthaicu = invoice.Trangthai,
            Trangthaimoi = IssuedStatus,
            Thoigian = now,
            Nguoidungid = currentUserService.UserId
        };

        await invoiceRepository.UpdatePublishedAsync(
            request.InvoiceId,
            soHoadon,
            history,
            now,
            currentUserService.UserId,
            cancellationToken);

        return new PublishInvoiceResultDto
        {
            Id = request.InvoiceId,
            Trangthai = IssuedStatus,
            SoHoadon = soHoadon
        };
    }

    private static string GenerateSoHoadon(DateTime issuedAt)
    {
        // Mock số hóa đơn theo format: năm + số thứ tự
        var sequence = (issuedAt.Ticks % 9_999_999) + 1;
        return sequence.ToString("D7");
    }
}
