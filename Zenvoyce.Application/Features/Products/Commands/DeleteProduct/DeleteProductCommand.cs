using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<Unit>;

public class DeleteProductCommandHandler(
    IProductRepository productRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeleteProductCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hàng hóa.");

        if (await productRepository.IsUsedInInvoiceAsync(product.Id, cancellationToken))
        {
            throw new InvalidOperationException("Không thể xóa hàng hóa vì đã tồn tại trong chi tiết hóa đơn.");
        }

        product.IsDeleted = true;
        product.UpdatedAt = dateTimeProvider.UtcNow;
        product.UpdatedBy = currentUserService.UserId;

        await productRepository.UpdateAsync(product, cancellationToken);
        return Unit.Value;
    }
}
