using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Invoices.Commands.CancelInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.CreateInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.ForwardInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.PublishInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.SignInvoice;
using Zenvoyce.Application.Features.Invoices.Commands.VerifyInvoiceXmlSignature;
using Zenvoyce.Application.Features.Invoices.Commands.SendInvoiceEmail;
using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Application.Features.Invoices.Queries.GetInvoiceHistory;
using Zenvoyce.Application.Features.Invoices.Queries.GetInvoices;
using Zenvoyce.Application.Features.Invoices.Queries.GetSalesReport;
using Zenvoyce.Application.Features.Invoices.Queries.GetSignedInvoiceXml;
using Zenvoyce.Application.Features.Invoices.Queries.PreviewInvoicePdf;
using Zenvoyce.Application.Features.Invoices.Queries.ExportInvoices;
using Zenvoyce.Application.Features.Invoices.Queries.ExportSalesReport;

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
        return Ok(ApiResponse<CreateInvoiceResultDto>.Ok(result));
    }

    [HttpPost("{id:guid}/adjust")]
    public async Task<ActionResult<ApiResponse<CreateInvoiceResultDto>>> CreateAdjustmentInvoice(Guid id, [FromBody] CreateInvoiceCommand command)
    {
        var result = await mediator.Send(command with { ThamChieuHoadonId = id });
        return Ok(ApiResponse<CreateInvoiceResultDto>.Ok(result));
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

    [HttpGet("reports/sales/export/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] Guid? donviId,
        [FromQuery] Guid? khachhangId,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExportSalesReportQuery(donviId, khachhangId, tuNgay, denNgay), cancellationToken);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{result.Filename}\"";
        return File(result.ExcelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Filename);
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

    [AllowAnonymous]
    [HttpPost("verify-signature-xml")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<VerifyInvoiceXmlSignatureResultDto>>> VerifyInvoiceXmlSignature(
        [FromForm] VerifyInvoiceXmlSignatureRequest request,
        CancellationToken cancellationToken)
    {
        if (request.XmlFile?.Length <= 0)
        {
            return BadRequest(ApiResponse<VerifyInvoiceXmlSignatureResultDto>.Fail("Vui lòng tải lên file XML hóa đơn."));
        }

        string xmlContent;
        using (var reader = new StreamReader(request.XmlFile!.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            xmlContent = await reader.ReadToEndAsync(cancellationToken);
        }

        var result = await mediator.Send(
            new VerifyInvoiceXmlSignatureCommand(xmlContent, request.XmlFile!.FileName),
            cancellationToken);
        return Ok(ApiResponse<VerifyInvoiceXmlSignatureResultDto>.Ok(result));
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

    [HttpGet("export/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportInvoices(
        [FromQuery] Guid? khachhangId,
        [FromQuery] string? trangthai,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExportInvoicesQuery(khachhangId, trangthai, tuNgay, denNgay), cancellationToken);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{result.Filename}\"";
        return File(result.ExcelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Filename);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvoiceHistoryItemDto>>>> GetInvoiceHistory(Guid id)
    {
        var result = await mediator.Send(new GetInvoiceHistoryQuery(id));
        return Ok(ApiResponse<IReadOnlyCollection<InvoiceHistoryItemDto>>.Ok(result));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}/preview-pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> PreviewPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PreviewInvoicePdfQuery(id), cancellationToken);
        Response.Headers.ContentDisposition = $"inline; filename=\"{result.Filename}\"";
        return File(result.PdfBytes, result.ContentType);
    }

    [AllowAnonymous]
    [HttpPost("preview-pdf-from-data")]
    [Produces("application/pdf")]
    public async Task<IActionResult> PreviewPdfFromData([FromBody] Zenvoyce.Application.Features.Invoices.Queries.PreviewInvoicePdfFromData.PreviewInvoicePdfFromDataQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        Response.Headers.ContentDisposition = $"inline; filename=\"{result.Filename}\"";
        return File(result.PdfBytes, result.ContentType);
    }

    [AllowAnonymous]
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvoiceListItemDto>>>> LookupInvoices(
        [FromQuery] string? soHoadon,
        [FromQuery] string? maSoThue)
    {
        var result = await mediator.Send(new Zenvoyce.Application.Features.Invoices.Queries.LookupInvoice.LookupInvoiceQuery(soHoadon, maSoThue));
        return Ok(ApiResponse<IReadOnlyCollection<InvoiceListItemDto>>.Ok(result));
    }

    /// <summary>Tải file XML hoá đơn đã ký số theo id hoặc số hoá đơn kèm các trường lọc (ký hiệu, MST khách, ngày lập).</summary>
    [AllowAnonymous]
    [HttpGet("signed-xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> GetSignedInvoiceXml(
        [FromQuery] Guid? id,
        [FromQuery] string? soHoadon,
        [FromQuery] string? kyHieu,
        [FromQuery] string? maSoThue,
        [FromQuery] DateTime? ngayLap,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetSignedInvoiceXmlQuery(id, soHoadon, kyHieu, maSoThue, ngayLap),
            cancellationToken);
        var bytes = Encoding.UTF8.GetBytes(result.XmlContent);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{result.Filename}\"";
        return File(bytes, "application/xml", result.Filename);
    }
}

public record CancelInvoiceRequest(string LyDo);
public sealed record VerifyInvoiceXmlSignatureRequest(IFormFile? XmlFile);
