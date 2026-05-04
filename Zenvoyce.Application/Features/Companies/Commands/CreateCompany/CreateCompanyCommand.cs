using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Companies.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Companies.Commands.CreateCompany;

public record CreateCompanyCommand(
    string Masothue,
    string Tendonvi,
    string? Diachi,
    string? Dienthoai,
    string? Nguoidaidien,
    string? Emailcongty,
    int? BankId,
    string? BankAccount) : IRequest<CompanyDto>;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Masothue)
            .NotEmpty()
            .Matches(@"^[0-9-]{10,14}$");
        RuleFor(x => x.Tendonvi).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Diachi).MaximumLength(500);
        RuleFor(x => x.Dienthoai).MaximumLength(20);
        RuleFor(x => x.Nguoidaidien).MaximumLength(100);
        RuleFor(x => x.Emailcongty).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Emailcongty));
        RuleFor(x => x.BankAccount).MaximumLength(50);
    }
}

public class CreateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var taxCode = request.Masothue.Trim();
        if (await companyRepository.TaxCodeExistsAsync(taxCode, null, cancellationToken))
        {
            throw new InvalidOperationException("Mã số thuế công ty đã tồn tại.");
        }

        var now = dateTimeProvider.UtcNow;
        var company = new Ttcty
        {
            Id = Guid.NewGuid(),
            Masothue = taxCode,
            Tendonvi = request.Tendonvi.Trim(),
            Diachi = request.Diachi?.Trim(),
            Dienthoai = request.Dienthoai?.Trim(),
            Nguoidaidien = request.Nguoidaidien?.Trim(),
            Emailcongty = string.IsNullOrWhiteSpace(request.Emailcongty) ? null : request.Emailcongty.Trim(),
            BankId = request.BankId,
            BankAccount = string.IsNullOrWhiteSpace(request.BankAccount) ? null : request.BankAccount.Trim(),
            Trangthai = 1,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        await companyRepository.AddAsync(company, cancellationToken);

        return CompanyDto.FromDomain(company);
    }
}
