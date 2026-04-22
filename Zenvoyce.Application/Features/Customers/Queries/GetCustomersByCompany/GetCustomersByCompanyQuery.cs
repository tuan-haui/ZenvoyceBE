using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Customers.DTOs;

namespace Zenvoyce.Application.Features.Customers.Queries.GetCustomersByCompany;

public record GetCustomersByCompanyQuery(Guid DonviId, string? Keyword) : IRequest<IReadOnlyCollection<CustomerDto>>;

public class GetCustomersByCompanyQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomersByCompanyQuery, IReadOnlyCollection<CustomerDto>>
{
    public async Task<IReadOnlyCollection<CustomerDto>> Handle(GetCustomersByCompanyQuery request, CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetByCompanyAsync(request.DonviId, request.Keyword, cancellationToken);
        return customers.Select(x => new CustomerDto
        {
            Id = x.Id,
            Donviid = x.Donviid,
            Tenkhachhang = x.Tenkhachhang,
            Masothue = x.Masothue,
            Email = x.Email,
            Dienthoai = x.Dienthoai
        }).ToArray();
    }
}
