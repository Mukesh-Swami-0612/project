namespace Ecom.Workflow.Application.DTOs;

public class WorkflowLogQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Action { get; set; }
    public string? Username { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
