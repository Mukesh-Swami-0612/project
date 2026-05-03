using Ecom.Workflow.Application.Commands;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Ecom.Workflow.Application.Services;

/// <summary>
/// Core orchestration engine - the brain of the workflow system
/// Reads current state, decides next step, updates state, triggers actions
/// This is where choreography becomes orchestration
/// </summary>
public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IWorkflowInstanceRepository _repo;
    private readonly ICommandPublisher _commandPublisher;
    private readonly IWorkflowAuditService _auditService;
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<WorkflowOrchestrator> _logger;
    private readonly ProductSagaOrchestrator _sagaOrchestrator;

    public WorkflowOrchestrator(
        IWorkflowInstanceRepository repo,
        ICommandPublisher commandPublisher,
        IWorkflowAuditService auditService,
        IOutboxRepository outbox,
        ILogger<WorkflowOrchestrator> logger,
        ProductSagaOrchestrator sagaOrchestrator)
    {
        _repo = repo;
        _commandPublisher = commandPublisher;
        _auditService = auditService;
        _outbox = outbox;
        _logger = logger;
        _sagaOrchestrator = sagaOrchestrator;
    }

    /// <summary>
    /// Main orchestration entry point
    /// Reads workflow state and decides what to do next
    /// </summary>
    public async Task ProcessAsync(Guid workflowId)
    {
        var workflow = await _repo.GetByIdAsync(workflowId);

        if (workflow == null)
        {
            _logger.LogError("WORKFLOW_NOT_FOUND | WorkflowId: {WorkflowId}", workflowId);
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        _logger.LogInformation(
            "WORKFLOW_PROCESSING | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Status: {Status} | Step: {Step} | CorrelationId: {CorrelationId}",
            workflow.Id,
            workflow.ProductId,
            workflow.Status,
            workflow.CurrentStep,
            workflow.CorrelationId);

        try
        {
            // 🧠 DECISION ENGINE: Route to appropriate handler based on current step
            switch (workflow.CurrentStep)
            {
                case WorkflowStep.Created:
                    await MoveToValidation(workflow);
                    break;

                case WorkflowStep.ValidationCompleted:
                    await MoveToApproval(workflow);
                    break;

                case WorkflowStep.Approved:
                    await MoveToPublishing(workflow);
                    break;

                case WorkflowStep.Published:
                    await CompleteWorkflow(workflow);
                    break;

                case WorkflowStep.Rejected:
                    await HandleRejection(workflow);
                    break;

                case WorkflowStep.Failed:
                    _logger.LogWarning(
                        "WORKFLOW_ALREADY_FAILED | WorkflowId: {WorkflowId} | LastError: {LastError}",
                        workflow.Id,
                        workflow.LastError);
                    break;

                case WorkflowStep.Completed:
                    _logger.LogInformation(
                        "WORKFLOW_ALREADY_COMPLETED | WorkflowId: {WorkflowId}",
                        workflow.Id);
                    break;

                default:
                    _logger.LogWarning(
                        "WORKFLOW_NO_ACTION | WorkflowId: {WorkflowId} | Step: {Step}",
                        workflow.Id,
                        workflow.CurrentStep);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "WORKFLOW_PROCESSING_FAILED | WorkflowId: {WorkflowId} | Step: {Step} | Error: {Error}",
                workflow.Id,
                workflow.CurrentStep,
                ex.Message);

            await MarkAsFailed(workflow, ex.Message);
            
            // 🔥 SAGA COMPENSATION: Trigger rollback on failure
            await _sagaOrchestrator.CompensateAsync(workflow.Id, ex.Message);
            
            throw;
        }
    }

    /// <summary>
    /// Transition: Created → ValidationPending
    /// Triggers product validation process by sending command to Catalog
    /// </summary>
    private async Task MoveToValidation(WorkflowInstance workflow)
    {
        _logger.LogInformation(
            "WORKFLOW_TRANSITION | WorkflowId: {WorkflowId} | From: {FromStep} | To: {ToStep}",
            workflow.Id,
            workflow.CurrentStep,
            WorkflowStep.ValidationPending);

        workflow.CurrentStep = WorkflowStep.ValidationPending;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(workflow);

        // 🔥 AUDIT LOG: Step transition
        await _auditService.LogAsync(
            "STEP_CHANGE",
            workflow.Id,
            workflow.ProductId,
            $"Moved to {WorkflowStep.ValidationPending}");

        _logger.LogInformation(
            "WORKFLOW_STEP_CHANGED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | NewStep: ValidationPending | CorrelationId: {CorrelationId}",
            workflow.Id,
            workflow.ProductId,
            workflow.CorrelationId);

        // 🔥 SEND COMMAND: Instruct Catalog to validate product
        var command = new ValidateProductCommand
        {
            ProductId = workflow.ProductId,
            CorrelationId = workflow.CorrelationId ?? Guid.NewGuid().ToString()
        };

        await _commandPublisher.PublishAsync("validate.product", command);

        // 🔥 AUDIT LOG: Command sent
        await _auditService.LogAsync(
            "COMMAND_SENT",
            workflow.Id,
            workflow.ProductId,
            "ValidateProduct command sent");

        _logger.LogInformation(
            "WORKFLOW_COMMAND_SENT | Command: ValidateProduct | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
            workflow.ProductId,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Transition: ValidationCompleted → ApprovalPending
    /// Triggers approval request process by sending command to Catalog
    /// </summary>
    private async Task MoveToApproval(WorkflowInstance workflow)
    {
        _logger.LogInformation(
            "WORKFLOW_TRANSITION | WorkflowId: {WorkflowId} | From: {FromStep} | To: {ToStep}",
            workflow.Id,
            workflow.CurrentStep,
            WorkflowStep.ApprovalPending);

        workflow.CurrentStep = WorkflowStep.ApprovalPending;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(workflow);

        // 🔥 AUDIT LOG: Step transition
        await _auditService.LogAsync(
            "STEP_CHANGE",
            workflow.Id,
            workflow.ProductId,
            $"Moved to {WorkflowStep.ApprovalPending}");

        _logger.LogInformation(
            "WORKFLOW_STEP_CHANGED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | NewStep: ApprovalPending | CorrelationId: {CorrelationId}",
            workflow.Id,
            workflow.ProductId,
            workflow.CorrelationId);

        // 🔥 SEND COMMAND: Instruct Catalog to request approval
        var command = new RequestApprovalCommand
        {
            ProductId = workflow.ProductId,
            CorrelationId = workflow.CorrelationId ?? Guid.NewGuid().ToString()
        };

        await _commandPublisher.PublishAsync("request.approval", command);

        // 🔥 AUDIT LOG: Command sent
        await _auditService.LogAsync(
            "COMMAND_SENT",
            workflow.Id,
            workflow.ProductId,
            "RequestApproval command sent");

        _logger.LogInformation(
            "WORKFLOW_COMMAND_SENT | Command: RequestApproval | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
            workflow.ProductId,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Transition: Approved → Publishing
    /// Triggers product publish process by sending command to Catalog
    /// </summary>
    private async Task MoveToPublishing(WorkflowInstance workflow)
    {
        _logger.LogInformation(
            "WORKFLOW_TRANSITION | WorkflowId: {WorkflowId} | From: {FromStep} | To: {ToStep}",
            workflow.Id,
            workflow.CurrentStep,
            WorkflowStep.Publishing);

        workflow.CurrentStep = WorkflowStep.Publishing;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(workflow);

        // 🔥 AUDIT LOG: Step transition
        await _auditService.LogAsync(
            "STEP_CHANGE",
            workflow.Id,
            workflow.ProductId,
            $"Moved to {WorkflowStep.Publishing}");

        _logger.LogInformation(
            "WORKFLOW_STEP_CHANGED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | NewStep: Publishing | CorrelationId: {CorrelationId}",
            workflow.Id,
            workflow.ProductId,
            workflow.CorrelationId);

        // 🔥 SEND COMMAND: Instruct Catalog to publish product
        var command = new PublishProductCommand
        {
            ProductId = workflow.ProductId,
            CorrelationId = workflow.CorrelationId ?? Guid.NewGuid().ToString()
        };

        await _commandPublisher.PublishAsync("publish.product", command);

        // 🔥 AUDIT LOG: Command sent
        await _auditService.LogAsync(
            "COMMAND_SENT",
            workflow.Id,
            workflow.ProductId,
            "PublishProduct command sent");

        _logger.LogInformation(
            "WORKFLOW_COMMAND_SENT | Command: PublishProduct | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
            workflow.ProductId,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Transition: Published → Completed
    /// Marks workflow as successfully completed
    /// </summary>
    private async Task CompleteWorkflow(WorkflowInstance workflow)
    {
        _logger.LogInformation(
            "WORKFLOW_COMPLETING | WorkflowId: {WorkflowId} | ProductId: {ProductId}",
            workflow.Id,
            workflow.ProductId);

        workflow.CurrentStep = WorkflowStep.Completed;
        workflow.Status = WorkflowStatus.Completed;
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(workflow);

        // 🔥 AUDIT LOG: Workflow completed
        await _auditService.LogAsync(
            "WORKFLOW_COMPLETED",
            workflow.Id,
            workflow.ProductId,
            $"Workflow completed successfully in {(workflow.CompletedAt.Value - workflow.CreatedAt).TotalSeconds:F2}s");

        // 🔥 PUBLISH COMPLETION EVENT: Notify other services
        await PublishWorkflowCompletedEvent(workflow);

        _logger.LogInformation(
            "WORKFLOW_COMPLETED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Duration: {Duration}s | CorrelationId: {CorrelationId}",
            workflow.Id,
            workflow.ProductId,
            (workflow.CompletedAt.Value - workflow.CreatedAt).TotalSeconds,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Handle rejection - workflow ends in failed state
    /// </summary>
    private async Task HandleRejection(WorkflowInstance workflow)
    {
        _logger.LogWarning(
            "WORKFLOW_REJECTED | WorkflowId: {WorkflowId} | ProductId: {ProductId}",
            workflow.Id,
            workflow.ProductId);

        workflow.Status = WorkflowStatus.Failed;
        workflow.CurrentStep = WorkflowStep.Failed;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.LastError = "Product was rejected during approval";

        await _repo.UpdateAsync(workflow);

        // 🔥 AUDIT LOG: Workflow rejected
        await _auditService.LogAsync(
            "WORKFLOW_REJECTED",
            workflow.Id,
            workflow.ProductId,
            "Product was rejected during approval");

        // 🔥 SAGA COMPENSATION: Trigger rollback for rejection
        await _sagaOrchestrator.CompensateAsync(workflow.Id, "Product was rejected during approval");

        // 🔥 PUBLISH FAILURE EVENT: Notify other services
        await PublishWorkflowFailedEvent(workflow, "Product was rejected during approval");

        _logger.LogInformation(
            "WORKFLOW_MARKED_FAILED | WorkflowId: {WorkflowId} | Reason: Rejected | CorrelationId: {CorrelationId}",
            workflow.Id,
            workflow.CorrelationId);
    }

    /// <summary>
    /// Mark workflow as failed due to error with retry logic
    /// Implements exponential backoff strategy
    /// </summary>
    private async Task MarkAsFailed(WorkflowInstance workflow, string error)
    {
        workflow.LastError = error;
        workflow.RetryCount++;
        workflow.UpdatedAt = DateTime.UtcNow;

        // 🔥 RETRY STRATEGY: Exponential backoff
        if (workflow.RetryCount >= workflow.MaxRetries)
        {
            // 🔴 DEAD LETTER: Max retries exhausted
            workflow.Status = WorkflowStatus.Failed;
            workflow.CurrentStep = WorkflowStep.Failed;
            workflow.NextRetryAt = null;

            // 🔥 AUDIT LOG: Workflow failed permanently
            await _auditService.LogAsync(
                "WORKFLOW_FAILED",
                workflow.Id,
                workflow.ProductId,
                $"Max retries exhausted ({workflow.MaxRetries}): {error}");

            // 🔥 PUBLISH FAILURE EVENT: Notify other services
            await PublishWorkflowFailedEvent(workflow, $"Max retries exhausted ({workflow.MaxRetries}): {error}");

            _logger.LogError(
                "WORKFLOW_DEAD_LETTER | WorkflowId: {WorkflowId} | ProductId: {ProductId} | RetryCount: {RetryCount} | MaxRetries: {MaxRetries} | Error: {Error}",
                workflow.Id,
                workflow.ProductId,
                workflow.RetryCount,
                workflow.MaxRetries,
                error);
        }
        else
        {
            // 🔥 SCHEDULE RETRY: Exponential backoff (2^retryCount seconds)
            var delaySeconds = Math.Pow(2, workflow.RetryCount);
            workflow.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);

            // 🔥 AUDIT LOG: Retry scheduled
            await _auditService.LogAsync(
                "RETRY_SCHEDULED",
                workflow.Id,
                workflow.ProductId,
                $"Retry {workflow.RetryCount}/{workflow.MaxRetries} scheduled for {workflow.NextRetryAt:yyyy-MM-dd HH:mm:ss}: {error}");

            _logger.LogWarning(
                "WORKFLOW_RETRY_SCHEDULED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | RetryCount: {RetryCount} | NextRetryAt: {NextRetryAt} | Error: {Error}",
                workflow.Id,
                workflow.ProductId,
                workflow.RetryCount,
                workflow.NextRetryAt,
                error);
        }

        await _repo.UpdateAsync(workflow);
    }

    /// <summary>
    /// Publish WorkflowFailedEvent to outbox for notification and reporting
    /// </summary>
    private async Task PublishWorkflowFailedEvent(WorkflowInstance workflow, string error)
    {
        var failedEvent = new Domain.Events.WorkflowFailedEvent
        {
            WorkflowId = workflow.Id,
            ProductId = workflow.ProductId,
            Error = error,
            CorrelationId = workflow.CorrelationId ?? string.Empty,
            OccurredAt = DateTime.UtcNow
        };

        await _outbox.AddAsync(new Domain.Entities.OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "workflow.failed",
            Payload = System.Text.Json.JsonSerializer.Serialize(failedEvent)
        });

        _logger.LogInformation(
            "WORKFLOW_FAILED_EVENT_PUBLISHED | WorkflowId: {WorkflowId} | ProductId: {ProductId}",
            workflow.Id,
            workflow.ProductId);
    }

    /// <summary>
    /// Publish WorkflowCompletedEvent to outbox for reporting
    /// </summary>
    private async Task PublishWorkflowCompletedEvent(WorkflowInstance workflow)
    {
        var completedEvent = new Domain.Events.WorkflowCompletedEvent
        {
            WorkflowId = workflow.Id,
            ProductId = workflow.ProductId,
            WorkflowType = "ProductWorkflow",
            EntityId = workflow.ProductId,
            RetryCount = workflow.RetryCount,
            CorrelationId = workflow.CorrelationId ?? string.Empty,
            CreatedAt = workflow.CreatedAt,
            CompletedAt = workflow.CompletedAt ?? DateTime.UtcNow,
            OccurredAt = DateTime.UtcNow
        };

        await _outbox.AddAsync(new Domain.Entities.OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "workflow.completed",
            Payload = System.Text.Json.JsonSerializer.Serialize(completedEvent)
        });

        _logger.LogInformation(
            "WORKFLOW_COMPLETED_EVENT_PUBLISHED | WorkflowId: {WorkflowId} | ProductId: {ProductId}",
            workflow.Id,
            workflow.ProductId);
    }
}
