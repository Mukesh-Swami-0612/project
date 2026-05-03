namespace Ecom.Reporting.Application.DTOs;

public class DailyNotificationTrendDto
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
}
