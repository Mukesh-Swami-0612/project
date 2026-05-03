using Ecom.Workflow.Application.Commands;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecom.Workflow.Application.Services;

/// <summary>
/// Saga orchestrator for Product lifecycle workflow
/// Implements compensation logic for distributed transaction rollback
/// Works alongside existing WorkflowOrchestrator
/// </summary>
public class ProductSagaOrchestrator
{
    private readonly IWorkflowInstanceRepository _workflowRepo;
    private readonly ISagaCompensationRepository _compensationRepo;
    private readonly ICommandPublisher _commandPublisher;
    private readonly IWorkflowAuditService _auditService;
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<ProductSagaOrchestrator> _logger;

    public ProductSagaOrchestrator(
        IWorkflowInstanceRepository workflowRepo,
        ISagaCompensationRepository compensationRepo,
        ICommandPublisher commandPublisher,
        IWorkflowAuditService auditService,
        IOutboxRepository outbox,
        ILogger<ProductSagaOrchestrator> logger)
    {
        _workflowRepo = workflowRepo;
        _compensationRepo = compensationRepo;
        _commandPublisher = commandPublisher;
        _auditService = auditService;
        _outbox = outbox;
        _logger = logger;
    }

    /// <summary>
    /// Execute compensation logic when saga fails
    /// Implements backward recovery pattern
    /// </summary>
    public async Task CompensateAsync(Guid workflowId, string failureReason)
    {
        var workflow = await _workflowRepo.GetByIdAsync(workflowId);

        if (workflow == null)
        {
            _logger.LogError("SAGA_COMPENSATION_FAILED | WorkflowId: {WorkflowId} | Reason: Workflow not found", workflowId);
            return;
        }

        _logger.LogWarning(
            "SAGA_COMPENSATION_STARTED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | FailedAtStep: {Step} | Reason: {Reason}",
            workflow.Id,
            workflow.ProductId,
            workflow.CurrentStep,
            failureReason);

        // 🔥 AUDIT LOG: Saga compensation started
        await _auditService.LogAsync(
            "SAGA_COMPENSATION_STARTED",
            workflow.Id,
            workflow.ProductId,
            $"Starting compensation for failure at {workflow.CurrentStep}: {failureReason}");

        // 🧠 DECISION: Determine compensation actions based on failed step
        var compensationActions = DetermineCompensationActions(workflow.CurrentStep);

        foreach (var action in compensationActions)
        {
            await ExecuteCompensationAction(workflow, action, failureReason);
        }

        // 🔥 MARK SAGA AS COMPENSATED
        workflow.Status = WorkflowStatus.Failed;
        workflow.CurrentStep = WorkflowStep.Failed;
        workflow.LastError = $"Saga compensated: {failureReason}";
        workflow.UpdatedAt = DateTime.UtcNow;

        await _workflowRepo.UpdateAsync(workflow);

        _logger.LogInformation(
            "SAGA_COMPENSATION_COMPLETED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | ActionsExecuted: {ActionCount}",
            workflow.Id,
            workflow.ProductId,
            compensationActions.Count);
    }

    /// <summary>
    /// Determine which compensation actions to execute based on failed step
    /// Implements backward recovery - undo actions in reverse order
    /// </summary>
    private List<SagaCompensationAction> DetermineCompensationActions(WorkflowStep failedStep)
    {
        var actions = new List<SagaCompensationAction>();

        switch (failedStep)
        {
            case WorkflowStep.Publishing:
            case WorkflowStep.Published:
                // Failed during/after publish - revert publish, approval, validation
                actions.Add(SagaCompensationAction.RevertPublish);
                actions.Add(SagaCompensationAction.RevertApproval);
                actions.Add(SagaCompensationAction.RevertValidation);
                actions.Add(SagaCompensationAction.NotifyFailure);
                break;

            case WorkflowStep.Approved:
            case WorkflowStep.ApprovalPending:
                // Failed during approval - revert approval, validation
                actions.Add(SagaCompensationAction.RevertApproval);
                actions.Add(SagaCompensationAction.RevertValidation);
                actions.Add(SagaCompensationAction.NotifyFailure);
                break;

            case WorkflowStep.ValidationCompleted:
            case WorkflowStep.ValidationPending:
                // Failed during validation - revert validation only
                actions.Add(SagaCompensationAction.RevertValidation);
                actions.Add(SagaCompensationAction.NotifyFailure);
                break;

            case WorkflowStep.Rejected:
                // Product was rejected - just notify
                actions.Add(SagaCompensationAction.NotifyFailure);
                break;

            default:
                // Early failure - just notify
                actions.Add(SagaCompensationAction.NotifyFailure);
                break;
        }

        return actions;
    }

    /// <summary>
    /// Execute a single compensation action
    /// </summary>
    private async Task ExecuteCompensationAction(
        WorkflowInstance workflow,
        SagaCompensationAction action,
        string failureReason)
    {
        var log = new SagaCompensationLog
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            ProductId = workflow.ProductId,
            FailedAtStep = workflow.CurrentStep,
            CompensationAction = action,
            CorrelationId = workflow.CorrelationId
        };

        try
        {
            _logger.LogInformation(
                "SAGA_COMPENSATION_ACTION | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Action: {Action}",
                workflow.Id,
                workflow.ProductId,
                action);

            switch (action)
            {
                case SagaCompensationAction.RevertPublish:
                    await RevertPublish(workflow);
                    log.CompensationDetails = "Sent UnpublishProduct command to Catalog";
                    break;

                case SagaCompensationAction.RevertApproval:
                    await RevertApproval(workflow);
                    log.CompensationDetails = "Sent RevertApproval command to Catalog";
                    break;

                case SagaCompensationAction.RevertValidation:
                    await RevertValidation(workflow);
                    log.CompensationDetails = "Sent RevertValidation command to Catalog";
                    break;

                case SagaCompensationAction.NotifyFailure:
                    await NotifyFailure(workflow, failureReason);
                    log.CompensationDetails = "Published ProductRejected event for notifications";
                    break;

                default:
                    log.CompensationDetails = "No action required";
                    break;
            }

            log.CompensationSuccessful = true;

            _logger.LogInformation(
                "SAGA_COMPENSATION_ACTION_SUCCESS | WorkflowId: {WorkflowId} | Action: {Action}",
                workflow.Id,
                action);
        }
        catch (Exception ex)
        {
            log.CompensationSuccessful = false;
            log.CompensationError = ex.Message;

            _logger.LogError(
                ex,
                "SAGA_COMPENSATION_ACTION_FAILED | WorkflowId: {WorkflowId} | Action: {Action} | Error: {Error}",
                workflow.Id,
                action,
                ex.Message);
        }

        await _compensationRepo.AddAsync(log);

        // 🔥 AUDIT LOG: Compensation action executed
        await _auditService.LogAsync(
            "SAGA_COMPENSATION_ACTION",
            workflow.Id,
            workflow.ProductId,
            $"{action}: {(log.CompensationSuccessful ? "SUCCESS" : "FAILED")} - {log.CompensationDetails ?? log.CompensationError}");
    }

    /// <summary>
    /// Compensation: Revert product publish
    /// Sends command to Catalog to unpublish product
    /// </summary>
    private async Task RevertPublish(WorkflowInstance workflow)
    {
        var command = new UnpublishProductCommand
        {
            ProductId = workflow.ProductId,
            Reason = "Saga compensation - workflow failed",
            CorrelationId = workflow.CorrelationId ?? Guid.NewGuid().ToString()
        };

        await _commandPublisher.PublishAsync("unpublish.product", command);

        _logger.LogInformation(
            "SAGA_REVERT_PUBLISH | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
            workflow.ProductId,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Compensation: Revert product approval
    /// Sends command to Catalog to revert approval status
    /// </summary>
    private async Task RevertApproval(WorkflowInstance workflow)
    {
        var command = new RevertApprovalCommand
        {
            ProductId = workflow.ProductId,
            Reason = "Saga compensation - workflow failed",
            CorrelationId = workflow.CorrelationId ?? Guid.NewGuid().ToString()
        };

        await _commandPublisher.PublishAsync("revert.approval", command);

        _logger.LogInformation(
            "SAGA_REVERT_APPROVAL | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
            workflow.ProductId,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Compensation: Revert product validation
    /// Sends command to Catalog to revert validation status
    /// </summary>
    private async Task RevertValidation(WorkflowInstance workflow)
    {
        var command = new RevertValidationCommand
        {
            ProductId = workflow.ProductId,
            Reason = "Saga compensation - workflow failed",
            CorrelationId = workflow.CorrelationId ?? Guid.NewGuid().ToString()
        };

        await _commandPublisher.PublishAsync("revert.validation", command);

        _logger.LogInformation(
            "SAGA_REVERT_VALIDATION | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
            workflow.ProductId,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Compensation: Notify about workflow failure
    /// Publishes ProductRejected event for notification service
    /// </summary>
    private async Task NotifyFailure(WorkflowInstance workflow, string reason)
    {
        var rejectedEvent = new Domain.Events.ProductRejectedEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = workflow.ProductId,
            RejectedBy = 0, // System rejection
            Reason = $"Workflow failed: {reason}",
            CorrelationId = workflow.CorrelationId ?? string.Empty,
            OccurredAt = DateTime.UtcNow
        };

        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "product.rejected",
            Payload = System.Text.Json.JsonSerializer.Serialize(rejectedEvent)
        });

        _logger.LogInformation(
            "SAGA_NOTIFY_FAILURE | ProductId: {ProductId} | Reason: {Reason}",
            workflow.ProductId,
            reason);
    }
}
