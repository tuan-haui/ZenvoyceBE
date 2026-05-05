using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Products.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Tenhanghoa,
    string? Sku,
    string? Donvitinh,
    decimal Dongia,
    decimal Thuesuat) : IRequest<ProductDto>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Tenhanghoa).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Sku).MaximumLength(20);
        RuleFor(x => x.Donvitinh).MaximumLength(50);
        RuleFor(x => x.Dongia).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Thuesuat).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductCommandHandler(
    IProductRepository productRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hàng hóa.");

        var productName = request.Tenhanghoa.Trim();
        if (await productRepository.NameExistsInCompanyAsync(product.Donviid, productName, product.Id, cancellationToken))
        {
            throw new InvalidOperationException("Tên hàng hóa đã tồn tại trong công ty.");
        }

        product.Tenhanghoa = productName;
        product.Sku = request.Sku?.Trim();
        product.Donvitinh = request.Donvitinh?.Trim();
        product.Dongia = request.Dongia;
        product.Thuesuat = request.Thuesuat;
        product.UpdatedAt = dateTimeProvider.UtcNow;
        product.UpdatedBy = currentUserService.UserId;

        await productRepository.UpdateAsync(product, cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            Donviid = product.Donviid,
            Tenhanghoa = product.Tenhanghoa,
            Sku = product.Sku,
            Donvitinh = product.Donvitinh,
            Dongia = product.Dongia,
            Thuesuat = product.Thuesuat
        };
    }
}
