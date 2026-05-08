using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Services;

namespace Zenvoyce.Application.Features.Invoices.Commands.VerifyInvoiceXmlSignature;

public record VerifyInvoiceXmlSignatureCommand(string XmlContent, string FileName) : IRequest<VerifyInvoiceXmlSignatureResultDto>;

public class VerifyInvoiceXmlSignatureResultDto
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public string SignerSubject { get; init; } = string.Empty;
    public string CertificateSerialNumber { get; init; } = string.Empty;
    public DateTime? SignedAtUtc { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = [];
}

public class VerifyInvoiceXmlSignatureCommandValidator : AbstractValidator<VerifyInvoiceXmlSignatureCommand>
{
    public VerifyInvoiceXmlSignatureCommandValidator()
    {
        RuleFor(x => x.XmlContent)
            .NotEmpty().WithMessage("Nội dung XML không được để trống.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Tên file XML không hợp lệ.")
            .Must(IsXmlFile).WithMessage("Chỉ chấp nhận file có phần mở rộng .xml.");
    }

    private static bool IsXmlFile(string fileName) =>
        fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
}

public class VerifyInvoiceXmlSignatureCommandHandler(IXmlInvoiceSigner xmlInvoiceSigner)
    : IRequestHandler<VerifyInvoiceXmlSignatureCommand, VerifyInvoiceXmlSignatureResultDto>
{
    public Task<VerifyInvoiceXmlSignatureResultDto> Handle(VerifyInvoiceXmlSignatureCommand request, CancellationToken cancellationToken)
    {
        var verifyResult = xmlInvoiceSigner.Verify(request.XmlContent);
        return Task.FromResult(new VerifyInvoiceXmlSignatureResultDto
        {
            IsValid = verifyResult.IsValid,
            Message = verifyResult.Message,
            SignerSubject = verifyResult.SignerSubject,
            CertificateSerialNumber = verifyResult.CertificateSerialNumber,
            SignedAtUtc = verifyResult.SignedAtUtc,
            Errors = verifyResult.Errors
        });
    }
}
