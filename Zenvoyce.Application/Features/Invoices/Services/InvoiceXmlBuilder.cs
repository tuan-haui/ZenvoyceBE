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
                new XElement("TongThanhToan", invoice.Tongthanhtoan.ToString(inv))
            )
        );

        return doc.ToString(SaveOptions.None);
    }
}
