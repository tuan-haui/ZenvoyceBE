namespace Zenvoyce.Application.Abstractions.Services;

public interface IXmlInvoiceSigner
{
    SignedXmlInvoiceResult Sign(string xmlContent);
    VerifyXmlInvoiceResult Verify(string xmlContent);
}

public class SignedXmlInvoiceResult
{
    public string SignedXml { get; set; } = string.Empty;
    public DateTime SignedAtUtc { get; set; }
    public string SignerSubject { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
}

public class VerifyXmlInvoiceResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SignerSubject { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public DateTime? SignedAtUtc { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = [];
}
