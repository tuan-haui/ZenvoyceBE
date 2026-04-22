using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Templates.Commands.ApplyTemplate;
using Zenvoyce.Application.Features.Templates.Commands.CreateBaseTemplate;
using Zenvoyce.Application.Features.Templates.Commands.NotifyTaxAuthority;
using Zenvoyce.Application.Features.Templates.Commands.UpdateBaseTemplate;
using Zenvoyce.Application.Features.Templates.Queries.GetCompanyTemplates;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
public class TemplatesController(ISender mediator) : ControllerBase
{
    [HttpPost("api/templates/base")]
    public async Task<IActionResult> CreateBaseTemplate([FromBody] CreateBaseTemplateCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(UpdateBaseTemplate), new { id = result.Id }, result);
    }

    [HttpPut("api/templates/base/{id:guid}")]
    public async Task<IActionResult> UpdateBaseTemplate(Guid id, [FromBody] UpdateBaseTemplateCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(result);
    }

    [HttpPost("api/templates/company/apply")]
    public async Task<IActionResult> ApplyTemplate([FromBody] ApplyTemplateCommand command)
    {
        var result = await mediator.Send(command);
        return Created("api/templates/company/apply", result);
    }

    [HttpGet("api/templates/company")]
    public async Task<IActionResult> GetCompanyTemplates(
        [FromQuery] Guid donviId,
        [FromQuery] string? kyhieuMau,
        [FromQuery] string? loaiHoadon,
        [FromQuery] short? trangthaiPhatHanh)
    {
        var result = await mediator.Send(new GetCompanyTemplatesQuery(donviId, kyhieuMau, loaiHoadon, trangthaiPhatHanh));
        return Ok(result);
    }

    [HttpPost("api/templates/company/{id:guid}/notify-tax")]
    public async Task<IActionResult> NotifyTaxAuthority(Guid id)
    {
        var result = await mediator.Send(new NotifyTaxAuthorityCommand(id));
        return Ok(result);
    }
}
