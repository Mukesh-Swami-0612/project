namespace Ecom.Workflow.Application.Commands;

/// <summary>
/// Command to unpublish a product (saga compensation)
/// </summary>
public class UnpublishProductCommand
{
    public int ProductId { get; set; }
    public string? Reason { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
