namespace Ecom.Reporting.Application.DTOs;

public class DailyProductTrendDto
{
    public DateTime Date { get; set; }
    public int Published { get; set; }
    public int Draft { get; set; }
    public int PendingApproval { get; set; }
    public int Rejected { get; set; }
}
