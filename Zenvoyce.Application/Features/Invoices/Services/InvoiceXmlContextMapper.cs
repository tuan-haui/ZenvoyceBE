using System.Globalization;
using System.Xml.Linq;

namespace Zenvoyce.Application.Features.Invoices.Services;

/// <summary>
/// Map XML metadata sang context object để Handlebars render template HTML.
/// Tên các placeholder phải đồng bộ với template (vd. mauhoadon1.html).
/// </summary>
public static class InvoiceXmlContextMapper
{
    public static IDictionary<string, object?> Map(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new InvalidOperationException("XML metadata không có phần tử gốc.");

        var seller = root.Element("NguoiBan");
        var buyer = root.Element("NguoiMua");
        var internalSignInfo = root.Element("KySoNoiBo");
        var hasSignature = root.Elements().Any(x => x.Name.LocalName == "Signature");
        var signedAtUtc = internalSignInfo?.Element("SignedAtUtc")?.Value;
        var signerSubject = internalSignInfo?.Element("SignerSubject")?.Value;
        var certificateSerial = internalSignInfo?.Element("CertificateSerial")?.Value;

        var items = root.Element("DanhSachHangHoa")?
            .Elements("HangHoa")
            .Select(MapItem)
            .ToList()
            ?? new List<IDictionary<string, object?>>();

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["symbol"] = root.Element("KyHieu")?.Value,
            ["invoice_number"] = root.Element("SoHoaDon")?.Value,
            ["issue_date"] = FormatDate(root.Element("NgayLap")?.Value),
            ["status"] = root.Element("TrangThai")?.Value,
            ["is_signed"] = hasSignature,
            ["signed_at"] = FormatDateTime(signedAtUtc),
            ["signed_at_utc"] = signedAtUtc ?? string.Empty,
            ["signer_subject"] = signerSubject ?? string.Empty,
            ["certificate_serial"] = certificateSerial ?? string.Empty,
            ["seller"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = seller?.Element("Ten")?.Value,
                ["tax_code"] = seller?.Element("MST")?.Value,
                ["address"] = seller?.Element("DiaChi")?.Value,
                ["phone"] = seller?.Element("DienThoai")?.Value,
                ["email"] = seller?.Element("Email")?.Value
            },
            ["buyer"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = buyer?.Element("Ten")?.Value,
                ["tax_code"] = buyer?.Element("MST")?.Value,
                ["email"] = buyer?.Element("Email")?.Value,
                ["phone"] = buyer?.Element("DienThoai")?.Value
            },
            ["items"] = items,
            ["total_amount"] = FormatMoney(root.Element("TongThanhToan")?.Value),
            ["sub_total"] = FormatMoney(root.Element("TongTienHang")?.Value),
            ["tax_total"] = FormatMoney(root.Element("TienThue")?.Value)
        };
    }

    private static IDictionary<string, object?> MapItem(XElement element)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = element.Element("Ten")?.Value,
            ["unit"] = element.Element("DonViTinh")?.Value,
            ["quantity"] = FormatNumber(element.Element("SoLuong")?.Value),
            ["price"] = FormatMoney(element.Element("DonGia")?.Value),
            ["tax_rate"] = FormatNumber(element.Element("ThueSuat")?.Value),
            ["amount"] = FormatMoney(element.Element("ThanhTien")?.Value)
        };
    }

    private static string FormatDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
        {
            return date.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        }

        return value;
    }

    private static string FormatMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "0";
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        }

        return value;
    }

    private static string FormatDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
        {
            return date.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.GetCultureInfo("vi-VN"));
        }

        return value;
    }

    private static string FormatNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "0";
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount % 1 == 0
                ? amount.ToString("0", CultureInfo.InvariantCulture)
                : amount.ToString("0.##", CultureInfo.InvariantCulture);
        }

        return value;
    }
}
