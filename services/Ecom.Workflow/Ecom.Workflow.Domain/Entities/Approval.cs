namespace Ecom.Workflow.Domain.Entities;

public class Approval
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ActionType { get; set; }
    public int? ApprovedBy { get; set; }
    public string? Comments { get; set; }
    public int? StepNumber { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
