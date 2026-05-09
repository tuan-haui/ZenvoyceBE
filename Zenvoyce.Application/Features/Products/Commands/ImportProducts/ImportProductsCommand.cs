using ClosedXML.Excel;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Products.Commands.ImportProducts;

public record ImportProductsCommand(Guid DonviId, Stream FileStream) : IRequest<int>;

public class ImportProductsCommandHandler(
    IProductRepository productRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ImportProductsCommand, int>
{
    public async Task<int> Handle(ImportProductsCommand request, CancellationToken cancellationToken)
    {
        if (!await productRepository.CompanyExistsAsync(request.DonviId, cancellationToken))
        {
            throw new KeyNotFoundException("Không tìm thấy công ty.");
        }

        var existingProducts = await productRepository.GetByCompanyAsync(request.DonviId, null, cancellationToken);
        var existingNames = existingProducts.Select(x => x.Tenhanghoa.ToLower()).ToHashSet();

        var products = new List<Danhmuchanghoa>();
        var now = dateTimeProvider.UtcNow;
        var userId = currentUserService.UserId;

        using (var workbook = new XLWorkbook(request.FileStream))
        {
            var worksheet = workbook.Worksheet(1);
            var rangeUsed = worksheet.RangeUsed();
            if (rangeUsed == null)
            {
                return 0;
            }

            var rows = rangeUsed.RowsUsed().Skip(1); // Skip header row

            foreach (var row in rows)
            {
                var tenHangHoa = row.Cell(1).GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(tenHangHoa))
                {
                    continue; 
                }

                if (existingNames.Contains(tenHangHoa.ToLower()))
                {
                    continue; // Bỏ qua trùng lặp
                }

                var sku = row.Cell(2).GetValue<string>()?.Trim();
                var donViTinh = row.Cell(3).GetValue<string>()?.Trim();
                var donGiaStr = row.Cell(4).GetValue<string>()?.Trim();
                var thueSuatStr = row.Cell(5).GetValue<string>()?.Trim();

                decimal.TryParse(donGiaStr, out var donGia);
                decimal.TryParse(thueSuatStr?.Replace("%", ""), out var thueSuat);

                products.Add(new Danhmuchanghoa
                {
                    Id = Guid.NewGuid(),
                    Donviid = request.DonviId,
                    Tenhanghoa = tenHangHoa,
                    Sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                    Donvitinh = string.IsNullOrWhiteSpace(donViTinh) ? null : donViTinh,
                    Dongia = donGia,
                    Thuesuat = thueSuat,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsDeleted = false
                });

                // Thêm vào danh sách đã tồn tại trong RAM để tránh trùng lặp chính trong file Excel
                existingNames.Add(tenHangHoa.ToLower());
            }
        }

        if (products.Count > 0)
        {
            await productRepository.AddRangeAsync(products, cancellationToken);
        }

        return products.Count;
    }
}
