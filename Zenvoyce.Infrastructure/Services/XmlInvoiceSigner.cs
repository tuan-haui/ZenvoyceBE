using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Microsoft.Extensions.Options;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Infrastructure.Options;

namespace Zenvoyce.Infrastructure.Services;

public class XmlInvoiceSigner : IXmlInvoiceSigner
{
    private readonly DigitalSignatureOptions options;
    private readonly Lazy<X509Certificate2> certificateLoader;

    public XmlInvoiceSigner(IOptions<DigitalSignatureOptions> options)
    {
        this.options = options.Value;
        certificateLoader = new Lazy<X509Certificate2>(LoadCertificate, true);
    }

    public SignedXmlInvoiceResult Sign(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            throw new InvalidOperationException("Nội dung XML rỗng, không thể ký số.");
        }

        var cert = certificateLoader.Value;
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlContent);

        var signedAtUtc = DateTime.UtcNow;
        var signerSubject = string.IsNullOrWhiteSpace(options.SignerName) ? cert.Subject : options.SignerName;
        var certificateSerial = cert.SerialNumber;

        AppendInternalSignatureMetadata(xmlDoc, signedAtUtc, signerSubject, certificateSerial);

        var signedXml = new SignedXml(xmlDoc)
        {
            SigningKey = cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("Không đọc được private key từ chứng thư.")
        };
        var signedInfo = signedXml.SignedInfo ?? throw new InvalidOperationException("Không khởi tạo được SignedInfo.");
        signedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var reference = new Reference(string.Empty);
        reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        var signature = signedXml.GetXml();
        xmlDoc.DocumentElement?.AppendChild(xmlDoc.ImportNode(signature, true));

        return new SignedXmlInvoiceResult
        {
            SignedXml = xmlDoc.OuterXml,
            SignedAtUtc = signedAtUtc,
            SignerSubject = signerSubject,
            CertificateSerialNumber = certificateSerial
        };
    }

    public VerifyXmlInvoiceResult Verify(string xmlContent)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            errors.Add("Nội dung XML rỗng.");
            return BuildInvalidResult(errors, "Xác thực chữ ký số thất bại.");
        }

        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        try
        {
            xmlDoc.LoadXml(xmlContent);
        }
        catch (XmlException)
        {
            errors.Add("Nội dung XML không hợp lệ.");
            return BuildInvalidResult(errors, "Xác thực chữ ký số thất bại.");
        }

        var signatureNode = FindSignatureNode(xmlDoc);
        if (signatureNode is null)
        {
            errors.Add("Không tìm thấy chữ ký số XMLDSig.");
            return BuildInvalidResult(errors, "Xác thực chữ ký số thất bại.");
        }

        var signedXml = new SignedXml(xmlDoc);
        try
        {
            signedXml.LoadXml(signatureNode);
        }
        catch (CryptographicException)
        {
            errors.Add("Không đọc được thông tin chữ ký số trong XML.");
            return BuildInvalidResult(errors, "Xác thực chữ ký số thất bại.");
        }

        var signingCertificate = ExtractSigningCertificate(signedXml);
        if (signingCertificate is null)
        {
            errors.Add("Không tìm thấy chứng thư số trong chữ ký XML.");
            return BuildInvalidResult(errors, "Xác thực chữ ký số thất bại.");
        }

        bool isValid;
        try
        {
            isValid = signedXml.CheckSignature(signingCertificate, verifySignatureOnly: true);
        }
        catch (CryptographicException)
        {
            errors.Add("Chữ ký số không hợp lệ hoặc dữ liệu XML đã bị thay đổi.");
            return BuildInvalidResult(errors, "Xác thực chữ ký số thất bại.", signingCertificate, TryReadSignedAtUtc(xmlDoc));
        }

        if (!isValid)
        {
            errors.Add("Chữ ký số không hợp lệ hoặc dữ liệu XML đã bị thay đổi.");
            return BuildInvalidResult(errors, "Xác thực chữ ký số thất bại.", signingCertificate, TryReadSignedAtUtc(xmlDoc));
        }

        return new VerifyXmlInvoiceResult
        {
            IsValid = true,
            Message = "Xác thực chữ ký số thành công.",
            SignerSubject = signingCertificate.Subject,
            CertificateSerialNumber = signingCertificate.SerialNumber,
            SignedAtUtc = TryReadSignedAtUtc(xmlDoc),
            Errors = []
        };
    }

    private static void AppendInternalSignatureMetadata(XmlDocument xmlDoc, DateTime signedAtUtc, string signerSubject, string certificateSerial)
    {
        var root = xmlDoc.DocumentElement ?? throw new InvalidOperationException("XML không có phần tử gốc để ký.");
        var metadataNode = root.SelectSingleNode("KySoNoiBo") as XmlElement ?? xmlDoc.CreateElement("KySoNoiBo");
        metadataNode.RemoveAll();

        var signedAtNode = xmlDoc.CreateElement("SignedAtUtc");
        signedAtNode.InnerText = signedAtUtc.ToString("O");
        metadataNode.AppendChild(signedAtNode);

        var signerNode = xmlDoc.CreateElement("SignerSubject");
        signerNode.InnerText = signerSubject;
        metadataNode.AppendChild(signerNode);

        var serialNode = xmlDoc.CreateElement("CertificateSerial");
        serialNode.InnerText = certificateSerial;
        metadataNode.AppendChild(serialNode);

        if (metadataNode.ParentNode is null)
        {
            root.AppendChild(metadataNode);
        }
    }

    private static VerifyXmlInvoiceResult BuildInvalidResult(
        IReadOnlyCollection<string> errors,
        string message,
        X509Certificate2? certificate = null,
        DateTime? signedAtUtc = null)
    {
        return new VerifyXmlInvoiceResult
        {
            IsValid = false,
            Message = message,
            SignerSubject = certificate?.Subject ?? string.Empty,
            CertificateSerialNumber = certificate?.SerialNumber ?? string.Empty,
            SignedAtUtc = signedAtUtc,
            Errors = errors
        };
    }

    private static XmlElement? FindSignatureNode(XmlDocument xmlDoc)
    {
        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
        return xmlDoc.SelectSingleNode("//ds:Signature", nsmgr) as XmlElement;
    }

    private static X509Certificate2? ExtractSigningCertificate(SignedXml signedXml)
    {
        foreach (var clause in signedXml.KeyInfo)
        {
            if (clause is not KeyInfoX509Data x509Data)
            {
                continue;
            }

            if (x509Data.Certificates is null)
            {
                continue;
            }

            foreach (var certificate in x509Data.Certificates)
            {
                if (certificate is X509Certificate cert)
                {
                    return new X509Certificate2(cert);
                }
            }
        }

        return null;
    }

    private static DateTime? TryReadSignedAtUtc(XmlDocument xmlDoc)
    {
        var signedAtText = xmlDoc.DocumentElement?.SelectSingleNode("KySoNoiBo/SignedAtUtc")?.InnerText;
        if (DateTime.TryParse(
            signedAtText,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var signedAtUtc))
        {
            return signedAtUtc;
        }

        return null;
    }

    private X509Certificate2 LoadCertificate()
    {
        if (string.IsNullOrWhiteSpace(options.PfxPath))
        {
            throw new InvalidOperationException("Thiếu cấu hình DigitalSignature:PfxPath.");
        }

        var fullPfxPath = ResolveFullPath(options.PfxPath);
        if (!File.Exists(fullPfxPath))
        {
            EnsurePfxFile(fullPfxPath);
        }

        var certBytes = File.ReadAllBytes(fullPfxPath);
        var cert = X509CertificateLoader.LoadPkcs12(
            certBytes,
            options.PfxPassword,
            X509KeyStorageFlags.EphemeralKeySet);
        if (!cert.HasPrivateKey)
        {
            throw new InvalidOperationException("Chứng thư không có private key để ký số.");
        }

        _ = cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("Chứng thư không hỗ trợ RSA private key.");
        return cert;
    }

    private string ResolveFullPath(string pfxPath)
    {
        if (Path.IsPathRooted(pfxPath))
        {
            return pfxPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, pfxPath));
    }

    private void EnsurePfxFile(string fullPfxPath)
    {
        if (string.IsNullOrWhiteSpace(options.PfxPassword))
        {
            throw new InvalidOperationException("Thiếu cấu hình DigitalSignature:PfxPassword để tạo chứng thư.");
        }

        var keySize = options.KeySize < 2048 ? 2048 : options.KeySize;
        var validYears = options.ValidYears <= 0 ? 1 : options.ValidYears;
        var distinguishedName = BuildDistinguishedName();

        var directory = Path.GetDirectoryName(fullPfxPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var rsa = RSA.Create(keySize);
        var request = new CertificateRequest(
            distinguishedName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
                critical: true));
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(validYears));

        var pfxBytes = cert.Export(X509ContentType.Pfx, options.PfxPassword);
        File.WriteAllBytes(fullPfxPath, pfxBytes);
    }

    private string BuildDistinguishedName()
    {
        var cn = EscapeDnValue(options.CommonName, "SimulatedInvoiceSigner");
        var o = EscapeDnValue(options.Organization, "Zenvoyce");
        var ou = EscapeDnValue(options.OrganizationalUnit, "Invoice");
        var c = EscapeDnValue(options.Country, "VN");
        return $"CN={cn}, O={o}, OU={ou}, C={c}";
    }

    private static string EscapeDnValue(string? value, string fallback)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return raw.Replace(",", "\\,");
    }
}
