namespace Ecom.Workflow.Application.DTOs;

public class ApprovalDto
{
    public int ProductId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int ApprovedBy { get; set; }
    public string? Comments { get; set; }
}
