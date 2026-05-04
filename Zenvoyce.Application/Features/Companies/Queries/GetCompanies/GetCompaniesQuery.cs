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
        return companies.Select(CompanyDto.FromDomain).ToArray();
    }
}
