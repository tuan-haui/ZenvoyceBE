using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Companies.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Companies.Commands.CreateCompany;

public record CreateCompanyCommand(string Masothue, string Tendonvi, string? Diachi, string? Dienthoai) : IRequest<CompanyDto>;

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
            Trangthai = 1,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        await companyRepository.AddAsync(company, cancellationToken);

        return new CompanyDto
        {
            Id = company.Id,
            Masothue = company.Masothue,
            Tendonvi = company.Tendonvi,
            Diachi = company.Diachi,
            Dienthoai = company.Dienthoai,
            Trangthai = company.Trangthai
        };
    }
}
