namespace Ecom.Reporting.Application.DTOs;

public class WorkflowReportDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Failed { get; set; }
    public int Cancelled { get; set; }
    public double CompletionRate { get; set; }
}
