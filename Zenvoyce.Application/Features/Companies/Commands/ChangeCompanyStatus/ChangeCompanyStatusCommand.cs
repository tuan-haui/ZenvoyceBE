using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Companies.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Companies.Commands.ChangeCompanyStatus;

public record ChangeCompanyStatusCommand(Guid Id, short Trangthai) : IRequest<CompanyDto>;

public class ChangeCompanyStatusCommandValidator : AbstractValidator<ChangeCompanyStatusCommand>
{
    public ChangeCompanyStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Trangthai).Must(x => x is 0 or 1);
    }
}

public class ChangeCompanyStatusCommandHandler(
    ICompanyRepository companyRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ChangeCompanyStatusCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(ChangeCompanyStatusCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy công ty.");

        company.Trangthai = request.Trangthai;
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
