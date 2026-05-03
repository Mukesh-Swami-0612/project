namespace Ecom.Reporting.Application.DTOs;

public class WorkflowPerformanceDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int InProgress { get; set; }

    public double CompletionRate { get; set; }
    public double FailureRate { get; set; }

    public double AvgRetryCount { get; set; }
    public int MaxRetryCount { get; set; }
}
