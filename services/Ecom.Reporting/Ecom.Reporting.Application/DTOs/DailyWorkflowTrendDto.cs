namespace Ecom.Reporting.Application.DTOs;

public class DailyWorkflowTrendDto
{
    public DateTime Date { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Failed { get; set; }
}
