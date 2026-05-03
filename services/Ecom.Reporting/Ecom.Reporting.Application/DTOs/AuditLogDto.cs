namespace Ecom.Reporting.Application.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public string? EntityName { get; set; }
    public int? EntityId { get; set; }
    public string? Action { get; set; }
    public string? EventType { get; set; }
    public string? SourceService { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
