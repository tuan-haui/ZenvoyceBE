using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Invoices.Commands.CancelInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.CreateInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.ForwardInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.PublishInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.SignInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.SendInvoiceEmail;
using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Application.Features.Invoices.Queries.GetInvoiceHistory;
using Zenvoyce.Application.Features.Invoices.Queries.GetInvoices;
using Zenvoyce.Application.Features.Invoices.Queries.GetSalesReport;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InvoicesController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateInvoiceResultDto>>> CreateInvoice([FromBody] CreateInvoiceCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetInvoiceHistory), new { id = result.Id }, ApiResponse<CreateInvoiceResultDto>.Ok(result));
    }

    [HttpPost("{id:guid}/adjust")]
    public async Task<ActionResult<ApiResponse<CreateInvoiceResultDto>>> CreateAdjustmentInvoice(Guid id, [FromBody] CreateInvoiceCommand command)
    {
        var result = await mediator.Send(command with { ThamChieuHoadonId = id });
        return CreatedAtAction(nameof(GetInvoiceHistory), new { id = result.Id }, ApiResponse<CreateInvoiceResultDto>.Ok(result));
    }

    [HttpPost("{id:guid}/send-email")]
    public async Task<ActionResult<ApiResponse<SendInvoiceEmailResultDto>>> SendInvoiceEmail(Guid id)
    {
        var result = await mediator.Send(new SendInvoiceEmailCommand(id));
        return Ok(ApiResponse<SendInvoiceEmailResultDto>.Ok(result));
    }

    [HttpGet("reports/sales")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SalesReportRow>>>> GetSalesReport(
        [FromQuery] Guid? donviId,
        [FromQuery] Guid? khachhangId,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
    {
        var result = await mediator.Send(new GetSalesReportQuery(donviId, khachhangId, tuNgay, denNgay));
        return Ok(ApiResponse<IReadOnlyCollection<SalesReportRow>>.Ok(result));
    }

    [HttpPost("{id:guid}/forward")]
    public async Task<ActionResult<ApiResponse<StringMessageDto>>> ForwardInvoice(Guid id)
    {
        await mediator.Send(new ForwardInvoiceCommand(id));
        return Ok(ApiResponse<StringMessageDto>.Ok(new StringMessageDto("Đã gửi hóa đơn chờ ký.")));
    }

    [HttpPost("{id:guid}/sign")]
    public async Task<ActionResult<ApiResponse<SignInvoiceResultDto>>> SignInvoice(Guid id)
    {
        var result = await mediator.Send(new SignInvoiceCommand(id));
        return Ok(ApiResponse<SignInvoiceResultDto>.Ok(result));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<PublishInvoiceResultDto>>> PublishInvoice(Guid id)
    {
        var result = await mediator.Send(new PublishInvoiceCommand(id));
        return Ok(ApiResponse<PublishInvoiceResultDto>.Ok(result));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<StringMessageDto>>> CancelInvoice(Guid id, [FromBody] CancelInvoiceRequest request)
    {
        await mediator.Send(new CancelInvoiceCommand(id, request.LyDo));
        return Ok(ApiResponse<StringMessageDto>.Ok(new StringMessageDto("Hóa đơn đã được hủy.")));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvoiceListItemDto>>>> GetInvoices(
        [FromQuery] Guid? khachhangId,
        [FromQuery] string? trangthai,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
    {
        var result = await mediator.Send(new GetInvoicesQuery(khachhangId, trangthai, tuNgay, denNgay));
        return Ok(ApiResponse<IReadOnlyCollection<InvoiceListItemDto>>.Ok(result));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvoiceHistoryItemDto>>>> GetInvoiceHistory(Guid id)
    {
        var result = await mediator.Send(new GetInvoiceHistoryQuery(id));
        return Ok(ApiResponse<IReadOnlyCollection<InvoiceHistoryItemDto>>.Ok(result));
    }
}

public record CancelInvoiceRequest(string LyDo);
