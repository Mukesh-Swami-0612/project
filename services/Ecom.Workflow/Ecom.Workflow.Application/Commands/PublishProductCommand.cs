namespace Ecom.Workflow.Application.Commands;

/// <summary>
/// Command sent from Workflow to Catalog to publish a product
/// </summary>
public class PublishProductCommand
{
    public int ProductId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
