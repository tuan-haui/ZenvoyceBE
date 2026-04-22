namespace Zenvoyce.Application.Features.SystemLogs.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? ActionType { get; set; }
    public DateTime? ActionTime { get; set; }
}
