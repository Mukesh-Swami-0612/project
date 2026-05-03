namespace Ecom.Reporting.Application.DTOs;

public class TopRejectedProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RejectionCount { get; set; }
    public string? MostCommonReason { get; set; }
}
