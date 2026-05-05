using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Invoices.Commands.SignInvoice;

public record SignInvoiceCommand(Guid InvoiceId) : IRequest<SignInvoiceResultDto>;

public class SignInvoiceResultDto
{
    public Guid Id { get; set; }
    public string Trangthai { get; set; } = string.Empty;
    public string XmlDaKy { get; set; } = string.Empty;
    public DateTime SignedAtUtc { get; set; }
    public string SignerSubject { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
}

public class SignInvoiceCommandValidator : AbstractValidator<SignInvoiceCommand>
{
    public SignInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}

public class SignInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IXmlInvoiceSigner xmlInvoiceSigner,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : IRequestHandler<SignInvoiceCommand, SignInvoiceResultDto>
{
    private const string SignedStatus = "Signed";

    public async Task<SignInvoiceResultDto> Handle(SignInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.Trangthai is not ("Draft" or "PendingSign"))
        {
            throw new InvalidOperationException($"Hóa đơn ở trạng thái '{invoice.Trangthai}' không thể ký số.");
        }

        if (string.IsNullOrWhiteSpace(invoice.XmlMetadata))
        {
            throw new InvalidOperationException("Hóa đơn chưa có XML metadata để ký số.");
        }

        var now = dateTimeProvider.UtcNow;
        var signingResult = xmlInvoiceSigner.Sign(invoice.XmlMetadata);
        var xmlDaky = signingResult.SignedXml;

        var history = new HoadonLichsu
        {
            Id = Guid.NewGuid(),
            Hoadonid = request.InvoiceId,
            Hanhdong = "Ký số hóa đơn",
            Trangthaicu = invoice.Trangthai,
            Trangthaimoi = SignedStatus,
            Thoigian = now,
            Nguoidungid = currentUserService.UserId
        };

        await invoiceRepository.UpdateSignedAsync(
            request.InvoiceId,
            xmlDaky,
            history,
            now,
            currentUserService.UserId,
            cancellationToken);

        return new SignInvoiceResultDto
        {
            Id = request.InvoiceId,
            Trangthai = SignedStatus,
            XmlDaKy = xmlDaky,
            SignedAtUtc = signingResult.SignedAtUtc,
            SignerSubject = signingResult.SignerSubject,
            CertificateSerialNumber = signingResult.CertificateSerialNumber
        };
    }
}
