namespace Ecom.Reporting.Application.DTOs;

public class NotificationReportDto
{
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public double SuccessRate { get; set; }
}
