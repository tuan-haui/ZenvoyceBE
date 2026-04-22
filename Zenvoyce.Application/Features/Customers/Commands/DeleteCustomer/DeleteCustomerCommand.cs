using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Customers.Commands.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id) : IRequest<Unit>;

public class DeleteCustomerCommandHandler(
    ICustomerRepository customerRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeleteCustomerCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy khách hàng.");

        if (await customerRepository.HasAnyInvoiceAsync(customer.Id, cancellationToken))
        {
            throw new InvalidOperationException("Không thể xóa khách hàng vì đã phát sinh hóa đơn.");
        }

        customer.IsDeleted = true;
        customer.UpdatedAt = dateTimeProvider.UtcNow;
        customer.UpdatedBy = currentUserService.UserId;

        await customerRepository.UpdateAsync(customer, cancellationToken);
        return Unit.Value;
    }
}
