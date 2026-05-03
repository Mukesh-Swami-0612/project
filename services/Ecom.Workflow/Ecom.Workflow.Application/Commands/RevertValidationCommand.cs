namespace Ecom.Workflow.Application.Commands;

/// <summary>
/// Command to revert product validation (saga compensation)
/// </summary>
public class RevertValidationCommand
{
    public int ProductId { get; set; }
    public string? Reason { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
