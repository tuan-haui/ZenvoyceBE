using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Companies.DTOs;

namespace Zenvoyce.Application.Features.Companies.Queries.GetCompanies;

public record GetCompaniesQuery : IRequest<IReadOnlyCollection<CompanyDto>>;

public class GetCompaniesQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetCompaniesQuery, IReadOnlyCollection<CompanyDto>>
{
    public async Task<IReadOnlyCollection<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companies = await companyRepository.GetAllAsync(cancellationToken);
        return companies.Select(x => new CompanyDto
        {
            Id = x.Id,
            Masothue = x.Masothue,
            Tendonvi = x.Tendonvi,
            Diachi = x.Diachi,
            Dienthoai = x.Dienthoai,
            Trangthai = x.Trangthai
        }).ToArray();
    }
}
