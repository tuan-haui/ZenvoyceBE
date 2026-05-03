using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Templates.Commands.ApplyTemplate;
using Zenvoyce.Application.Features.Templates.Commands.CreateBaseTemplate;
using Zenvoyce.Application.Features.Templates.Commands.NotifyTaxAuthority;
using Zenvoyce.Application.Features.Templates.Commands.UpdateBaseTemplate;
using Zenvoyce.Application.Features.Templates.DTOs;
using Zenvoyce.Application.Features.Templates.Queries.GetCompanyTemplates;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
public class TemplatesController(ISender mediator) : ControllerBase
{
    [HttpPost("api/templates/base")]
    public async Task<ActionResult<ApiResponse<BaseTemplateDto>>> CreateBaseTemplate([FromBody] CreateBaseTemplateCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(UpdateBaseTemplate), new { id = result.Id }, ApiResponse<BaseTemplateDto>.Ok(result));
    }

    [HttpPut("api/templates/base/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BaseTemplateDto>>> UpdateBaseTemplate(Guid id, [FromBody] UpdateBaseTemplateCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(ApiResponse<BaseTemplateDto>.Ok(result));
    }

    [HttpPost("api/templates/company/apply")]
    public async Task<ActionResult<ApiResponse<CompanyTemplateDto>>> ApplyTemplate([FromBody] ApplyTemplateCommand command)
    {
        var result = await mediator.Send(command);
        return Created("api/templates/company/apply", ApiResponse<CompanyTemplateDto>.Ok(result));
    }

    [HttpGet("api/templates/company")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CompanyTemplateDto>>>> GetCompanyTemplates(
        [FromQuery] Guid donviId,
        [FromQuery] string? kyhieuMau,
        [FromQuery] string? loaiHoadon,
        [FromQuery] short? trangthaiPhatHanh)
    {
        var result = await mediator.Send(new GetCompanyTemplatesQuery(donviId, kyhieuMau, loaiHoadon, trangthaiPhatHanh));
        return Ok(ApiResponse<IReadOnlyCollection<CompanyTemplateDto>>.Ok(result));
    }

    [HttpPost("api/templates/company/{id:guid}/notify-tax")]
    public async Task<ActionResult<ApiResponse<TemplateStatusHistoryDto>>> NotifyTaxAuthority(Guid id)
    {
        var result = await mediator.Send(new NotifyTaxAuthorityCommand(id));
        return Ok(ApiResponse<TemplateStatusHistoryDto>.Ok(result));
    }
}
