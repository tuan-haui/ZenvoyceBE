using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Templates.Commands.ApplyTemplate;
using Zenvoyce.Application.Features.Templates.Commands.CreateBaseTemplate;
using Zenvoyce.Application.Features.Templates.Commands.DeleteBaseTemplate;
using Zenvoyce.Application.Features.Templates.Commands.CancelTemplate;
using Zenvoyce.Application.Features.Templates.Commands.NotifyTaxAuthority;
using Zenvoyce.Application.Features.Templates.Commands.UpdateBaseTemplate;
using Zenvoyce.Application.Features.Templates.DTOs;
using Zenvoyce.Application.Features.Templates.Queries.GetBaseTemplateById;
using Zenvoyce.Application.Features.Templates.Queries.GetBaseTemplates;
using Zenvoyce.Application.Features.Templates.Queries.GetCompanyTemplates;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TemplatesController(ISender mediator) : ControllerBase
{
    [HttpGet("base")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BaseTemplateDto>>>> GetBaseTemplates()
    {
        var result = await mediator.Send(new GetBaseTemplatesQuery());
        return Ok(ApiResponse<IReadOnlyCollection<BaseTemplateDto>>.Ok(result));
    }

    [HttpGet("base/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BaseTemplateDto>>> GetBaseTemplateById(Guid id)
    {
        var result = await mediator.Send(new GetBaseTemplateByIdQuery(id));
        return Ok(ApiResponse<BaseTemplateDto>.Ok(result));
    }

    [HttpPost("base")]
    public async Task<ActionResult<ApiResponse<BaseTemplateDto>>> CreateBaseTemplate(
        [FromBody] CreateBaseTemplateCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<BaseTemplateDto>.Ok(result));
    }

    [HttpPut("base/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BaseTemplateDto>>> UpdateBaseTemplate(Guid id,
        [FromBody] UpdateBaseTemplateCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(ApiResponse<BaseTemplateDto>.Ok(result));
    }

    [HttpDelete("base/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteBaseTemplate(Guid id)
    {
        await mediator.Send(new DeleteBaseTemplateCommand(id));
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPost("company/apply")]
    public async Task<ActionResult<ApiResponse<CompanyTemplateDto>>> ApplyTemplate(
        [FromBody] ApplyTemplateCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<CompanyTemplateDto>.Ok(result));
    }

    [HttpGet("company")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CompanyTemplateDto>>>> GetCompanyTemplates(
        [FromQuery] Guid donviId,
        [FromQuery] string? kyhieuMau,
        [FromQuery] string? loaiHoadon,
        [FromQuery] short? trangthaiPhatHanh)
    {
        var result =
            await mediator.Send(new GetCompanyTemplatesQuery(donviId, kyhieuMau, loaiHoadon, trangthaiPhatHanh));
        return Ok(ApiResponse<IReadOnlyCollection<CompanyTemplateDto>>.Ok(result));
    }

    [HttpPost("notify-tax/{id:guid}")]
    public async Task<ActionResult<ApiResponse<TemplateStatusHistoryDto>>> NotifyTaxAuthority(Guid id)
    {
        var result = await mediator.Send(new NotifyTaxAuthorityCommand(id));
        return Ok(ApiResponse<TemplateStatusHistoryDto>.Ok(result));
    }

    [HttpPost("cancel/{id:guid}")]
    public async Task<ActionResult<ApiResponse<TemplateStatusHistoryDto>>> CancelTemplate(Guid id)
    {
        var result = await mediator.Send(new CancelTemplateCommand(id));
        return Ok(ApiResponse<TemplateStatusHistoryDto>.Ok(result));
    }
}
