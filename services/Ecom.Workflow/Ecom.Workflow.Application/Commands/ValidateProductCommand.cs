namespace Ecom.Workflow.Application.Commands;

/// <summary>
/// Command sent from Workflow to Catalog to validate a product
/// This is an INSTRUCTION, not an event
/// </summary>
public class ValidateProductCommand
{
    public int ProductId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
