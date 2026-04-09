using Ecom.Workflow.Application.DTOs;

namespace Ecom.Workflow.Application.Interfaces;

public interface IApprovalService
{
    Task SubmitForReviewAsync(int productId, int submittedBy);
    Task ApproveAsync(ApprovalDto dto);
    Task RejectAsync(ApprovalDto dto);
}
