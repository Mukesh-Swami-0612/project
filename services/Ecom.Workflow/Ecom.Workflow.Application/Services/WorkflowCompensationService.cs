using Ecom.Workflow.Application.Commands;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Ecom.Workflow.Application.Services;

/// <summary>
/// Handles workflow compensation (rollback) logic
/// Implements saga compensation patterns for failure recovery
/// </summary>
public class WorkflowCompensationService : IWorkflowCompensationService
{
    private readonly ICommandPublisher _commandPublisher;
    private readonly IWorkflowInstanceRepository _repo;
    private readonly ILogger<WorkflowCompensationService> _logger;

    public WorkflowCompensationService(
        ICommandPublisher commandPublisher,
        IWorkflowInstanceRepository repo,
        ILogger<WorkflowCompensationService> logger)
    {
        _commandPublisher = commandPublisher;
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// Compensate for publishing failure
    /// Reverts approval and resets workflow state
    /// </summary>
    public async Task CompensatePublishingFailureAsync(WorkflowInstance workflow, string reason)
    {
        _logger.LogWarning(
            "WORKFLOW_COMPENSATION_STARTED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Step: Publishing | Reason: {Reason}",
            workflow.Id,
            workflow.ProductId,
            reason);

        try
        {
            // 🔥 COMPENSATION: Revert approval
            var command = new RevertApprovalCommand
            {
                ProductId = workflow.ProductId,
                Reason = $"Publishing failed: {reason}",
                CorrelationId = workflow.CorrelationId ?? Guid.NewGuid().ToString()
            };

            await _commandPublisher.PublishAsync("revert.approval", command);

            // Update workflow state
            workflow.CurrentStep = WorkflowStep.ApprovalPending;
            workflow.LastError = $"Publishing failed, approval reverted: {reason}";
            workflow.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(workflow);

            _logger.LogInformation(
                "WORKFLOW_COMPENSATION_COMPLETED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | RevertedTo: ApprovalPending",
                workflow.Id,
                workflow.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "WORKFLOW_COMPENSATION_FAILED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Error: {Error}",
                workflow.Id,
                workflow.ProductId,
                ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Compensate for approval failure
    /// Resets workflow to validation completed state
    /// </summary>
    public async Task CompensateApprovalFailureAsync(WorkflowInstance workflow, string reason)
    {
        _logger.LogWarning(
            "WORKFLOW_COMPENSATION_STARTED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Step: Approval | Reason: {Reason}",
            workflow.Id,
            workflow.ProductId,
            reason);

        // Update workflow state
        workflow.CurrentStep = WorkflowStep.ValidationCompleted;
        workflow.LastError = $"Approval failed: {reason}";
        workflow.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(workflow);

        _logger.LogInformation(
            "WORKFLOW_COMPENSATION_COMPLETED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | RevertedTo: ValidationCompleted",
            workflow.Id,
            workflow.ProductId);
    }
}
