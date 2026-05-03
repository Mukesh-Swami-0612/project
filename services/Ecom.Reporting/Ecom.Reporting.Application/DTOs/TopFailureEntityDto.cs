namespace Ecom.Reporting.Application.DTOs;

public class TopFailureEntityDto
{
    public string EntityId { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public int FailureCount { get; set; }
    public string? MostCommonReason { get; set; }
}
