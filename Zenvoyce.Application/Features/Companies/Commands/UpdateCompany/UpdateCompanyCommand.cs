using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Companies.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Companies.Commands.UpdateCompany;

public record UpdateCompanyCommand(
    Guid Id,
    string Masothue,
    string Tendonvi,
    string? Diachi,
    string? Dienthoai) : IRequest<CompanyDto>;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Masothue)
            .NotEmpty()
            .Matches(@"^[0-9-]{10,14}$");
        RuleFor(x => x.Tendonvi).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Diachi).MaximumLength(500);
        RuleFor(x => x.Dienthoai).MaximumLength(20);
    }
}

public class UpdateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy công ty.");

        var normalizedTaxCode = request.Masothue.Trim();
        var normalizedName = request.Tendonvi.Trim();
        var hasIssuedInvoices = await companyRepository.HasAnyInvoiceAsync(company.Id, cancellationToken);
        if (hasIssuedInvoices && (company.Masothue != normalizedTaxCode || company.Tendonvi != normalizedName))
        {
            throw new InvalidOperationException("Không thể thay đổi mã số thuế hoặc tên công ty vì đã phát sinh hóa đơn.");
        }

        if (await companyRepository.TaxCodeExistsAsync(normalizedTaxCode, company.Id, cancellationToken))
        {
            throw new InvalidOperationException("Mã số thuế công ty đã tồn tại.");
        }

        company.Masothue = normalizedTaxCode;
        company.Tendonvi = normalizedName;
        company.Diachi = request.Diachi?.Trim();
        company.Dienthoai = request.Dienthoai?.Trim();
        company.UpdatedAt = dateTimeProvider.UtcNow;
        company.UpdatedBy = currentUserService.UserId;

        await companyRepository.UpdateAsync(company, cancellationToken);

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
