namespace Ecom.Workflow.Application.DTOs;

public class StatusUpdateDto
{
    public string Status { get; set; } = string.Empty;
    public int UpdatedBy { get; set; }
    public string? Remarks { get; set; }
}
