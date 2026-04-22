using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Customers.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid Id,
    string Tenkhachhang,
    string? Masothue,
    string? Email,
    string? Dienthoai) : IRequest<CustomerDto>;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Tenkhachhang).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Masothue)
            .Matches(@"^[0-9-]{10,14}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Masothue));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Dienthoai).MaximumLength(20);
    }
}

public class UpdateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy khách hàng.");

        var taxCode = request.Masothue?.Trim();
        if (!string.IsNullOrWhiteSpace(taxCode) &&
            await customerRepository.TaxCodeExistsInCompanyAsync(customer.Donviid, taxCode, customer.Id, cancellationToken))
        {
            throw new InvalidOperationException("Mã số thuế khách hàng đã tồn tại trong công ty.");
        }

        customer.Tenkhachhang = request.Tenkhachhang.Trim();
        customer.Masothue = taxCode;
        customer.Email = request.Email?.Trim();
        customer.Dienthoai = request.Dienthoai?.Trim();
        customer.UpdatedAt = dateTimeProvider.UtcNow;
        customer.UpdatedBy = currentUserService.UserId;

        await customerRepository.UpdateAsync(customer, cancellationToken);

        return new CustomerDto
        {
            Id = customer.Id,
            Donviid = customer.Donviid,
            Tenkhachhang = customer.Tenkhachhang,
            Masothue = customer.Masothue,
            Email = customer.Email,
            Dienthoai = customer.Dienthoai
        };
    }
}
