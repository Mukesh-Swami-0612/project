namespace Ecom.Workflow.Application.Commands;

/// <summary>
/// Command sent from Workflow to Catalog to request product approval
/// </summary>
public class RequestApprovalCommand
{
    public int ProductId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
