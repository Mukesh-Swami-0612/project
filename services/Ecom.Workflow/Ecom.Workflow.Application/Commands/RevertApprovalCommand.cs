namespace Ecom.Workflow.Application.Commands;

/// <summary>
/// Command to revert product approval (saga compensation)
/// </summary>
public class RevertApprovalCommand
{
    public int ProductId { get; set; }
    public string? Reason { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
