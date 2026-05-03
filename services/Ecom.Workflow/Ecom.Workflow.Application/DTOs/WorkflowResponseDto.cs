namespace Ecom.Workflow.Application.DTOs;

public class WorkflowResponseDto
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
