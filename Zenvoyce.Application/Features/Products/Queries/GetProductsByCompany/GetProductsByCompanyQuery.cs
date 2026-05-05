using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Products.DTOs;

namespace Zenvoyce.Application.Features.Products.Queries.GetProductsByCompany;

public record GetProductsByCompanyQuery(Guid DonviId, string? Keyword) : IRequest<IReadOnlyCollection<ProductDto>>;

public class GetProductsByCompanyQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsByCompanyQuery, IReadOnlyCollection<ProductDto>>
{
    public async Task<IReadOnlyCollection<ProductDto>> Handle(GetProductsByCompanyQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetByCompanyAsync(request.DonviId, request.Keyword, cancellationToken);
        return products.Select(x => new ProductDto
        {
            Id = x.Id,
            Donviid = x.Donviid,
            Tenhanghoa = x.Tenhanghoa,
            Sku = x.Sku,
            Donvitinh = x.Donvitinh,
            Dongia = x.Dongia,
            Thuesuat = x.Thuesuat
        }).ToArray();
    }
}
