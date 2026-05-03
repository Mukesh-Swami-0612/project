using Ecom.Workflow.Application.Events;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Telemetry;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecom.Workflow.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes product.validated events from Catalog service
/// Updates workflow to ValidationCompleted and triggers next step
/// </summary>
public class ProductValidatedConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductValidatedConsumer> _logger;

    public ProductValidatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductValidatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ProductValidatedEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = WorkflowActivitySource.Instance.StartActivity(
                "ProcessProductValidated",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = WorkflowActivitySource.Instance.StartActivity("ProcessProductValidated");
        }

        using (activity)
        {
            activity?.SetTag("event.id", message?.EventId);
            activity?.SetTag("product.id", message?.ProductId);

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();

            _logger.LogInformation(
                "Processing event {EventType} with EventId {EventId}",
                nameof(ProductValidatedEvent),
                message?.EventId);

            _logger.LogInformation(
                "WORKFLOW_RECEIVED_PRODUCT_VALIDATED | ProductId: {ProductId} | IsValid: {IsValid} | CorrelationId: {CorrelationId}",
                message.ProductId,
                message.IsValid,
                message.CorrelationId);

            try
            {
                var workflow = await repo.GetByProductIdAsync(message.ProductId);

                if (workflow == null)
                {
                    _logger.LogWarning(
                        "WORKFLOW_NOT_FOUND_FOR_VALIDATION | ProductId: {ProductId}",
                        message.ProductId);
                    return;
                }
                
                activity?.SetTag("workflow.id", workflow.Id);

                if (!message.IsValid)
                {
                    _logger.LogWarning(
                        "PRODUCT_VALIDATION_FAILED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Message: {Message}",
                        workflow.Id,
                        message.ProductId,
                        message.ValidationMessage);

                    workflow.Status = WorkflowStatus.Failed;
                    workflow.CurrentStep = WorkflowStep.Failed;
                    workflow.LastError = $"Validation failed: {message.ValidationMessage}";
                    workflow.UpdatedAt = DateTime.UtcNow;

                    await repo.UpdateAsync(workflow);
                    return;
                }

                // 🔥 UPDATE STATE: Move to ValidationCompleted
                workflow.CurrentStep = WorkflowStep.ValidationCompleted;
                workflow.UpdatedAt = DateTime.UtcNow;

                await repo.UpdateAsync(workflow);

                _logger.LogInformation(
                    "WORKFLOW_VALIDATION_COMPLETED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
                    workflow.Id,
                    workflow.ProductId,
                    workflow.CorrelationId);

                // 🔥 TRIGGER ORCHESTRATOR: Continue to next step
                await orchestrator.ProcessAsync(workflow.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "WORKFLOW_VALIDATION_PROCESSING_FAILED | ProductId: {ProductId} | Error: {Error}",
                    message.ProductId,
                    ex.Message);
                throw;
            }
        }
    }
}
