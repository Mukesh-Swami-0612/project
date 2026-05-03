namespace Ecom.Workflow.Application.DTOs;

public class WorkflowQueryDto
{
    public string? Status { get; set; }
    public string? Step { get; set; }
    public int? ProductId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
