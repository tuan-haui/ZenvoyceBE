using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Services;

/// <summary>
/// Seed mẫu hoá đơn HTML mặc định nếu DB chưa có mẫu nào.
/// Idempotent: chỉ insert khi không tồn tại record với ký hiệu MAU01.
/// </summary>
public class BaseTemplateSeeder
{
    private const string DefaultKyhieu = "MAU01";

    public static async Task SeedAsync(
        ZenvoyceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var html = DefaultMauhoadon1Html;
        var css = DefaultMauhoadon1Css;
        var now = DateTime.UtcNow;

        var existingTemplate = await dbContext.Mauhoadongocs
            .FirstOrDefaultAsync(x => x.Kyhieu == DefaultKyhieu, cancellationToken);

        if (existingTemplate != null)
        {
            if (existingTemplate.HtmlContent != html || existingTemplate.CssContent != css)
            {
                existingTemplate.HtmlContent = html;
                existingTemplate.CssContent = css;
                existingTemplate.UpdatedAt = now;
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Đã cập nhật mẫu hoá đơn mặc định '{Kyhieu}'.", DefaultKyhieu);
            }
            return;
        }



        dbContext.Mauhoadongocs.Add(new Entities.Mauhoadongoc
        {
            Id = Guid.NewGuid(),
            Tenmau = "Mẫu hóa đơn cơ bản",
            Loaihoadon = "GTGT",
            Kyhieu = DefaultKyhieu,
            HtmlContent = html,
            CssContent = css,
            Version = "1.0",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = null,
            UpdatedBy = null,
            IsDeleted = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Đã seed mẫu hoá đơn mặc định '{Kyhieu}'.", DefaultKyhieu);
    }

    private const string DefaultMauhoadon1Html = """
<div class="invoice">
    <div class="center bold">HÓA ĐƠN BÁN HÀNG</div>

    <div class="right">
        Ký hiệu: {{symbol}}<br />
        Số: {{invoice_number}}
    </div>

    <div class="section">
        Ngày: {{issue_date}}
    </div>

    <div class="section">
        <b>Người bán:</b> {{seller.name}}<br />
        MST: {{seller.tax_code}}<br />
        Địa chỉ: {{seller.address}}
    </div>

    <div class="section">
        <b>Người mua:</b> {{buyer.name}}<br />
        MST: {{buyer.tax_code}}
    </div>

    <table>
        <thead>
            <tr>
                <th>STT</th>
                <th>Tên hàng</th>
                <th>Số lượng</th>
                <th>Đơn giá</th>
                <th>Thành tiền</th>
            </tr>
        </thead>
        <tbody>
            {{#each items}}
            <tr>
                <td>{{@index}}</td>
                <td>{{name}}</td>
                <td>{{quantity}}</td>
                <td>{{price}}</td>
                <td>{{amount}}</td>
            </tr>
            {{/each}}
        </tbody>
    </table>

    <div class="section right">
        Tổng tiền: {{total_amount}}
    </div>

    <div class="signature">
        <div class="center">
            <b>Người mua</b><br />
            (Ký, ghi rõ họ tên)
        </div>
        <div class="center">
            <b>Người bán</b><br />
            (Ký, đóng dấu, ghi rõ họ tên)<br />
            {{#if is_signed}}
            <div class="signature-box">
                <b>Ký bởi:</b> {{signer_subject}}<br />
                <b>Thời gian ký:</b> {{signed_at}}<br />
                <b>Serial chứng thư:</b> {{certificate_serial}}
            </div>
            {{/if}}
        </div>
    </div>
</div>
""";

    private const string DefaultMauhoadon1Css = """
body {
    font-family: "Times New Roman", serif;
    font-size: 13px;
    margin: 0;
    padding: 0;
}

.invoice {
    width: 800px;
    margin: 0 auto;
    padding: 20px;
    border: 1px solid #000;
}

.center { text-align: center; }
.right { text-align: right; }
.bold { font-weight: bold; }

table {
    width: 100%;
    border-collapse: collapse;
    margin-top: 10px;
}

table, th, td { border: 1px solid #000; }

th, td {
    padding: 4px;
    font-size: 12px;
}

.no-border td { border: none; }

.section { margin-top: 10px; }

.signature {
    margin-top: 30px;
    display: flex;
    justify-content: space-between;
}

.signature-box {
    margin-top: 10px;
    padding: 10px;
    border: 2px solid #e74c3c;
    color: #e74c3c;
    text-align: left;
    display: inline-block;
    border-radius: 4px;
    background-color: #fdf2f2;
}
""";
}
