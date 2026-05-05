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
        var certificateSerial = cert.SerialNumber ?? string.Empty;

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
