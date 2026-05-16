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

        var issueDateStr = root.Element("NgayLap")?.Value;
        var issueDate = ParseDate(issueDateStr);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["symbol"] = root.Element("KyHieu")?.Value,
            ["form_number"] = root.Element("MauSo")?.Value,
            ["invoice_number"] = root.Element("SoHoaDon")?.Value,
            ["issue_date"] = FormatDate(issueDateStr),
            ["issue_day"] = issueDate?.Day.ToString("00") ?? "",
            ["issue_month"] = issueDate?.Month.ToString("00") ?? "",
            ["issue_year"] = issueDate?.Year.ToString() ?? "",
            ["status"] = root.Element("TrangThai")?.Value,
            ["is_signed"] = hasSignature,
            ["signed_at"] = FormatDateTime(signedAtUtc),
            ["signed_at_utc"] = signedAtUtc ?? string.Empty,
            ["signer_subject"] = signerSubject ?? string.Empty,
            ["certificate_serial"] = certificateSerial ?? string.Empty,
            ["payment_method"] = root.Element("HinhThucThanhToan")?.Value,
            ["seller"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = seller?.Element("Ten")?.Value,
                ["tax_code"] = seller?.Element("MST")?.Value,
                ["address"] = seller?.Element("DiaChi")?.Value,
                ["phone"] = seller?.Element("DienThoai")?.Value,
                ["email"] = seller?.Element("Email")?.Value,
                ["bank_account"] = seller?.Element("SoTaiKhoan")?.Value,
                ["bank_name"] = seller?.Element("TenNganHang")?.Value
            },
            ["buyer"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = buyer?.Element("Ten")?.Value,
                ["representative"] = buyer?.Element("NguoiMuaHang")?.Value ?? buyer?.Element("Ten")?.Value,
                ["tax_code"] = buyer?.Element("MST")?.Value,
                ["address"] = buyer?.Element("DiaChi")?.Value,
                ["email"] = buyer?.Element("Email")?.Value,
                ["phone"] = buyer?.Element("DienThoai")?.Value,
                ["bank_account"] = buyer?.Element("SoTaiKhoan")?.Value
            },
            ["items"] = items,
            ["total_amount"] = FormatMoney(root.Element("TongThanhToan")?.Value),
            ["sub_total"] = FormatMoney(root.Element("TongTienHang")?.Value),
            ["subtotal"] = FormatMoney(root.Element("TongTienHang")?.Value),
            ["tax_total"] = FormatMoney(root.Element("TienThue")?.Value),
            ["vat_amount"] = FormatMoney(root.Element("TienThue")?.Value),
            ["vat_rate"] = FormatNumber(root.Element("ThueSuat")?.Value),
            ["amount_in_words"] = GetAmountInWords(root)
        };
    }

    private static string GetAmountInWords(XElement root)
    {
        var amountInWords = root.Element("SoTienBangChu")?.Value;
        if (!string.IsNullOrWhiteSpace(amountInWords))
        {
            return amountInWords;
        }

        var totalAmount = root.Element("TongThanhToan")?.Value;
        if (string.IsNullOrWhiteSpace(totalAmount))
        {
            return string.Empty;
        }

        if (!decimal.TryParse(totalAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return string.Empty;
        }

        return ConvertMoneyToVietnameseWords(amount);
    }

    private static string ConvertMoneyToVietnameseWords(decimal amount)
    {
        var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (rounded == 0)
        {
            return "Không đồng";
        }

        var text = ConvertIntegerToVietnameseWords((long)Math.Abs(rounded));
        if (rounded < 0)
        {
            text = "âm " + text;
        }

        return char.ToUpper(text[0], CultureInfo.GetCultureInfo("vi-VN")) + text[1..] + " đồng";
    }

    private static string ConvertIntegerToVietnameseWords(long number)
    {
        if (number == 0)
        {
            return "không";
        }

        var scaleNames = new[]
        {
            string.Empty,
            "nghìn",
            "triệu",
            "tỷ",
            "nghìn tỷ",
            "triệu tỷ",
            "tỷ tỷ"
        };

        var parts = new List<string>();
        var scaleIndex = 0;

        while (number > 0)
        {
            var group = (int)(number % 1000);
            if (group > 0)
            {
                var isHighestGroup = (number / 1000 == 0); // không còn nhóm nào cao hơn
                var groupText = ConvertThreeDigitGroup(group, scaleIndex > 0 && !isHighestGroup);
                //                                            ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                var scaleText = scaleNames[scaleIndex];
                parts.Insert(0, string.IsNullOrWhiteSpace(scaleText) ? groupText : $"{groupText} {scaleText}");
            }

            number /= 1000;
            scaleIndex++;
        }

        return string.Join(" ", parts);
    }

    private static string ConvertThreeDigitGroup(int number, bool forceLeadingHundreds)
    {
        var hundreds = number / 100;
        var tens = (number % 100) / 10;
        var ones = number % 10;

        var parts = new List<string>();

        if (hundreds > 0)
        {
            parts.Add(UnitWord(hundreds));
            parts.Add("trăm");
        }
        else if (forceLeadingHundreds && (tens > 0 || ones > 0))
        {
            parts.Add("không trăm");
        }

        if (tens > 1)
        {
            parts.Add(UnitWord(tens));
            parts.Add("mươi");
        }
        else if (tens == 1)
        {
            parts.Add("mười");
        }
        else if ((hundreds > 0 || forceLeadingHundreds) && ones > 0)
        {
            parts.Add("lẻ");
        }

        if (ones > 0)
        {
            parts.Add(GetOnesWord(ones, tens));
        }

        return string.Join(" ", parts);
    }

    private static string UnitWord(int digit) => digit switch
    {
        1 => "một",
        2 => "hai",
        3 => "ba",
        4 => "bốn",
        5 => "năm",
        6 => "sáu",
        7 => "bảy",
        8 => "tám",
        9 => "chín",
        _ => string.Empty
    };

    private static string GetOnesWord(int ones, int tens) => ones switch
    {
        1 when tens > 1 => "mốt",
        4 when tens > 0 => "tư",
        5 when tens > 0 => "lăm",
        _ => UnitWord(ones)
    };

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
        {
            return date.ToLocalTime();
        }

        return null;
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
