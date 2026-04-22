using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Customers.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(
    Guid Donviid,
    string Tenkhachhang,
    string? Masothue,
    string? Email,
    string? Dienthoai) : IRequest<CustomerDto>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Donviid).NotEmpty();
        RuleFor(x => x.Tenkhachhang).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Masothue)
            .Matches(@"^[0-9-]{10,14}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Masothue));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Dienthoai).MaximumLength(20);
    }
}

public class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!await customerRepository.CompanyExistsAsync(request.Donviid, cancellationToken))
        {
            throw new KeyNotFoundException("Không tìm thấy công ty.");
        }

        var taxCode = request.Masothue?.Trim();
        if (!string.IsNullOrWhiteSpace(taxCode) &&
            await customerRepository.TaxCodeExistsInCompanyAsync(request.Donviid, taxCode, null, cancellationToken))
        {
            throw new InvalidOperationException("Mã số thuế khách hàng đã tồn tại trong công ty.");
        }

        var now = dateTimeProvider.UtcNow;
        var customer = new Ttkhachhang
        {
            Id = Guid.NewGuid(),
            Donviid = request.Donviid,
            Tenkhachhang = request.Tenkhachhang.Trim(),
            Masothue = taxCode,
            Email = request.Email?.Trim(),
            Dienthoai = request.Dienthoai?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        await customerRepository.AddAsync(customer, cancellationToken);

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
