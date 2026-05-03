namespace Ecom.Reporting.Domain.Entities;

public class ProductReadModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Status { get; set; } = string.Empty; // Published, Draft, PendingApproval, Rejected
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsLowStock { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
