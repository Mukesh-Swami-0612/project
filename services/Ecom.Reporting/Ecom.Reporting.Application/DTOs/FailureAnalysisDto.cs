namespace Ecom.Reporting.Application.DTOs;

public class FailureAnalysisDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
