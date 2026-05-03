namespace Ecom.Reporting.Application.DTOs;

public class ProductReportDto
{
    public int Total { get; set; }
    public int Published { get; set; }
    public int Draft { get; set; }
    public int PendingApproval { get; set; }
    public int Rejected { get; set; }
    public int LowStock { get; set; }
    public double PublishRate { get; set; }
}
