using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.SystemLogs.Queries.GetAuditLogs;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Route("api/system")]
[Authorize]
public class SystemController(ISender mediator) : ControllerBase
{
    [HttpGet("logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? userId,
        [FromQuery] string? actionType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(fromDate, toDate, userId, actionType, pageNumber, pageSize));
        return Ok(result);
    }
}
