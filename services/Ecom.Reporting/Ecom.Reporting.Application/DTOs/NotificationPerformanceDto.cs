namespace Ecom.Reporting.Application.DTOs;

public class NotificationPerformanceDto
{
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }

    public double SuccessRate { get; set; }
    public double FailureRate { get; set; }

    public double AvgRetryCount { get; set; }
    public int MaxRetryCount { get; set; }
}
