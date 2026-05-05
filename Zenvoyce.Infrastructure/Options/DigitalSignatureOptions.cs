namespace Zenvoyce.Infrastructure.Options;

public class DigitalSignatureOptions
{
    public const string SectionName = "DigitalSignature";

    public string PfxPath { get; set; } = string.Empty;
    public string PfxPassword { get; set; } = string.Empty;
    public string? SignerName { get; set; }
    public string CommonName { get; set; } = "SimulatedInvoiceSigner";
    public string Organization { get; set; } = "Zenvoyce";
    public string OrganizationalUnit { get; set; } = "Invoice";
    public string Country { get; set; } = "VN";
    public int ValidYears { get; set; } = 3;
    public int KeySize { get; set; } = 2048;
}
