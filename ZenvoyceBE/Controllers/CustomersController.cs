using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Customers.Commands.CreateCustomer;
using Zenvoyce.Application.Features.Customers.Commands.DeleteCustomer;
using Zenvoyce.Application.Features.Customers.Commands.UpdateCustomer;
using Zenvoyce.Application.Features.Customers.DTOs;
using Zenvoyce.Application.Features.Customers.Queries.GetCustomersByCompany;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
public class CustomersController(ISender mediator) : ControllerBase
{
    [HttpGet("api/companies/{donviId:guid}/customers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CustomerDto>>>> GetByCompany(Guid donviId, [FromQuery] string? keyword)
    {
        var result = await mediator.Send(new GetCustomersByCompanyQuery(donviId, keyword));
        return Ok(ApiResponse<IReadOnlyCollection<CustomerDto>>.Ok(result));
    }

    [HttpPost("api/customers")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetByCompany), new { donviId = result.Donviid }, ApiResponse<CustomerDto>.Ok(result));
    }

    [HttpPut("api/customers/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(Guid id, [FromBody] UpdateCustomerCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(ApiResponse<CustomerDto>.Ok(result));
    }

    [HttpDelete("api/customers/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id)
    {
        await mediator.Send(new DeleteCustomerCommand(id));
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
