using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Customers.Commands.CreateCustomer;
using Zenvoyce.Application.Features.Customers.Commands.DeleteCustomer;
using Zenvoyce.Application.Features.Customers.Commands.UpdateCustomer;
using Zenvoyce.Application.Features.Customers.Queries.GetCustomersByCompany;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
public class CustomersController(ISender mediator) : ControllerBase
{
    [HttpGet("api/companies/{donviId:guid}/customers")]
    public async Task<IActionResult> GetByCompany(Guid donviId, [FromQuery] string? keyword)
    {
        var result = await mediator.Send(new GetCustomersByCompanyQuery(donviId, keyword));
        return Ok(result);
    }

    [HttpPost("api/customers")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetByCompany), new { donviId = result.Donviid }, result);
    }

    [HttpPut("api/customers/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(result);
    }

    [HttpDelete("api/customers/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteCustomerCommand(id));
        return Ok();
    }
}
