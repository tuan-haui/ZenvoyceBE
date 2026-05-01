using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Invoices.Commands.CancelInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.CreateInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.ForwardInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.PublishInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.SignInvoice;
using Zenvoyce.Application.Features.Invoices.Queries.GetInvoiceHistory;
using Zenvoyce.Application.Features.Invoices.Queries.GetInvoices;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
public class InvoicesController(ISender mediator) : ControllerBase
{
    [HttpPost("api/invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetInvoiceHistory), new { id = result.Id }, result);
    }

    [HttpPost("api/invoices/{id:guid}/forward")]
    public async Task<IActionResult> ForwardInvoice(Guid id)
    {
        await mediator.Send(new ForwardInvoiceCommand(id));
        return Ok(new { Message = "Đã gửi hóa đơn chờ ký." });
    }

    [HttpPost("api/invoices/{id:guid}/sign")]
    public async Task<IActionResult> SignInvoice(Guid id)
    {
        var result = await mediator.Send(new SignInvoiceCommand(id));
        return Ok(result);
    }

    [HttpPost("api/invoices/{id:guid}/publish")]
    public async Task<IActionResult> PublishInvoice(Guid id)
    {
        var result = await mediator.Send(new PublishInvoiceCommand(id));
        return Ok(result);
    }

    [HttpPost("api/invoices/{id:guid}/cancel")]
    public async Task<IActionResult> CancelInvoice(Guid id, [FromBody] CancelInvoiceRequest request)
    {
        await mediator.Send(new CancelInvoiceCommand(id, request.LyDo));
        return Ok(new { Message = "Hóa đơn đã được hủy." });
    }

    [HttpGet("api/invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] Guid? khachhangId,
        [FromQuery] string? trangthai,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
    {
        var result = await mediator.Send(new GetInvoicesQuery(khachhangId, trangthai, tuNgay, denNgay));
        return Ok(result);
    }

    [HttpGet("api/invoices/{id:guid}/history")]
    public async Task<IActionResult> GetInvoiceHistory(Guid id)
    {
        var result = await mediator.Send(new GetInvoiceHistoryQuery(id));
        return Ok(result);
    }
}

public record CancelInvoiceRequest(string LyDo);
