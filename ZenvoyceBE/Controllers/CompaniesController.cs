using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Companies.Commands.ChangeCompanyStatus;
using Zenvoyce.Application.Features.Companies.Commands.CreateCompany;
using Zenvoyce.Application.Features.Companies.Commands.UpdateCompany;
using Zenvoyce.Application.Features.Companies.DTOs;
using Zenvoyce.Application.Features.Companies.Queries.GetCompanies;
using Zenvoyce.Application.Features.Companies.Queries.GetCompanyById;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CompanyDto>>>> GetCompanies()
    {
        var result = await mediator.Send(new GetCompaniesQuery());
        return Ok(ApiResponse<IReadOnlyCollection<CompanyDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetCompanyById(Guid id)
    {
        var result = await mediator.Send(new GetCompanyByIdQuery(id));
        return Ok(ApiResponse<CompanyDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Create([FromBody] CreateCompanyCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetCompanyById), new { id = result.Id }, ApiResponse<CompanyDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Update(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(ApiResponse<CompanyDto>.Ok(result));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> ChangeStatus(Guid id, [FromBody] ChangeCompanyStatusRequest request)
    {
        var result = await mediator.Send(new ChangeCompanyStatusCommand(id, request.Trangthai));
        return Ok(ApiResponse<CompanyDto>.Ok(result));
    }
}

public record ChangeCompanyStatusRequest(short Trangthai);
