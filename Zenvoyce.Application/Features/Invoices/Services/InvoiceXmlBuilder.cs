using System.Globalization;
using System.Xml.Linq;
using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Features.Invoices.Services;

/// <summary>
/// Sinh XML metadata cho hoá đơn dùng làm dữ liệu render template HTML.
/// Cấu trúc tuân theo Điều 10 NĐ 123/2020/NĐ-CP.
/// </summary>
public static class InvoiceXmlBuilder
{
    public static string Build(
        Hoadon invoice,
        IEnumerable<HoadonHanghoa> lines,
        IReadOnlyDictionary<Guid, Danhmuchanghoa> productMap,
        Ttcty seller,
        Ttkhachhang buyer)
    {
        var inv = CultureInfo.InvariantCulture;
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("HoaDon",
                new XElement("Id", invoice.Id),
                new XElement("KyHieu", invoice.Kyhieu ?? string.Empty),
                new XElement("SoHoaDon", invoice.Sohoadon ?? string.Empty),
                new XElement("NgayLap", invoice.Ngaylap.ToString("yyyy-MM-dd", inv)),
                new XElement("TrangThai", invoice.Trangthai),
                invoice.Thamchieuhoadonid.HasValue
                    ? new XElement("HoaDonThamChieu",
                        new XElement("Id", invoice.Thamchieuhoadonid.Value))
                    : null,
                new XElement("NguoiBan",
                    new XElement("Ten", seller.Tendonvi ?? string.Empty),
                    new XElement("MST", seller.Masothue ?? string.Empty),
                    new XElement("DiaChi", seller.Diachi ?? string.Empty),
                    new XElement("DienThoai", seller.Dienthoai ?? string.Empty),
                    new XElement("Email", seller.Emailcongty ?? string.Empty)
                ),
                new XElement("NguoiMua",
                    new XElement("Ten", buyer.Tenkhachhang ?? string.Empty),
                    new XElement("MST", buyer.Masothue ?? string.Empty),
                    new XElement("Email", buyer.Email ?? string.Empty),
                    new XElement("DienThoai", buyer.Dienthoai ?? string.Empty)
                ),
                   new XElement("ThueSuat", lines.FirstOrDefault()?.Thuesuat.ToString(inv) ?? "0"),
                   new XElement("DanhSachHangHoa",
                    lines.Select((line, index) =>
                    {
                        productMap.TryGetValue(line.Hanghoaid, out var product);
                        return new XElement("HangHoa",
                            new XAttribute("STT", index + 1),
                            new XElement("Ten", product?.Tenhanghoa ?? string.Empty),
                            new XElement("DonViTinh", product?.Donvitinh ?? string.Empty),
                            new XElement("SoLuong", line.Soluong.ToString(inv)),
                            new XElement("DonGia", line.Dongia.ToString(inv)),
                            new XElement("ThueSuat", line.Thuesuat.ToString(inv)),
                            new XElement("ThanhTien", line.Thanhtien.ToString(inv))
                        );
                    })
                ),
                new XElement("TongTienHang", invoice.Tongtien.ToString(inv)),
                new XElement("TienThue", invoice.Tienthue.ToString(inv)),
                new XElement("TongThanhToan", invoice.Tongthanhtoan.ToString(inv)),
                new XElement("SoTienBangChu", ConvertMoneyToVietnameseWords(invoice.Tongthanhtoan))
            )
        );

        return doc.ToString(SaveOptions.None);
    }

    private static string ConvertMoneyToVietnameseWords(decimal amount)
    {
        var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (rounded == 0)
        {
            return "không đồng";
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
                var groupText = ConvertThreeDigitGroup(group, scaleIndex > 0);
                var scaleText = scaleIndex < scaleNames.Length ? scaleNames[scaleIndex] : string.Empty;
                if (!string.IsNullOrWhiteSpace(scaleText))
                {
                    groupText = $"{groupText} {scaleText}";
                }

                parts.Insert(0, groupText);
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
}
