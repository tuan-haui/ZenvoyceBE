using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Companies.Commands.ChangeCompanyStatus;
using Zenvoyce.Application.Features.Companies.Commands.CreateCompany;
using Zenvoyce.Application.Features.Companies.Commands.UpdateCompany;
using Zenvoyce.Application.Features.Companies.Queries.GetCompanies;
using Zenvoyce.Application.Features.Companies.Queries.GetCompanyById;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        var result = await mediator.Send(new GetCompaniesQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCompanyById(Guid id)
    {
        var result = await mediator.Send(new GetCompanyByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetCompanyById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeCompanyStatusRequest request)
    {
        var result = await mediator.Send(new ChangeCompanyStatusCommand(id, request.Trangthai));
        return Ok(result);
    }
}

public record ChangeCompanyStatusRequest(short Trangthai);
