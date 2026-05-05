using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Products.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    Guid Donviid,
    string Tenhanghoa,
    string? Sku,
    string? Donvitinh,
    decimal Dongia,
    decimal Thuesuat) : IRequest<ProductDto>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Donviid).NotEmpty();
        RuleFor(x => x.Tenhanghoa).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Sku).MaximumLength(20);
        RuleFor(x => x.Donvitinh).MaximumLength(50);
        RuleFor(x => x.Dongia).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Thuesuat).GreaterThanOrEqualTo(0);
    }
}

public class CreateProductCommandHandler(
    IProductRepository productRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!await productRepository.CompanyExistsAsync(request.Donviid, cancellationToken))
        {
            throw new KeyNotFoundException("Không tìm thấy công ty.");
        }

        var productName = request.Tenhanghoa.Trim();
        if (await productRepository.NameExistsInCompanyAsync(request.Donviid, productName, null, cancellationToken))
        {
            throw new InvalidOperationException("Tên hàng hóa đã tồn tại trong công ty.");
        }

        var now = dateTimeProvider.UtcNow;
        var product = new Danhmuchanghoa
        {
            Id = Guid.NewGuid(),
            Donviid = request.Donviid,
            Tenhanghoa = productName,
            Sku = request.Sku?.Trim(),
            Donvitinh = request.Donvitinh?.Trim(),
            Dongia = request.Dongia,
            Thuesuat = request.Thuesuat,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        await productRepository.AddAsync(product, cancellationToken);

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
