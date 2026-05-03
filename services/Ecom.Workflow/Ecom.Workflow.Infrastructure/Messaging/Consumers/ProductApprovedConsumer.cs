using Ecom.Workflow.Application.Events;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Telemetry;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecom.Workflow.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes product.approved events from Catalog service
/// Updates workflow to Approved and triggers next step
/// </summary>
public class ProductApprovedConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductApprovedConsumer> _logger;

    public ProductApprovedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductApprovedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ProductApprovedEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = WorkflowActivitySource.Instance.StartActivity(
                "ProcessProductApproved",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = WorkflowActivitySource.Instance.StartActivity("ProcessProductApproved");
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
                nameof(ProductApprovedEvent),
                message?.EventId);

            _logger.LogInformation(
                "WORKFLOW_RECEIVED_PRODUCT_APPROVED | ProductId: {ProductId} | ApprovedBy: {ApprovedBy} | CorrelationId: {CorrelationId}",
                message.ProductId,
                message.ApprovedBy,
                message.CorrelationId);

            try
            {
                var workflow = await repo.GetByProductIdAsync(message.ProductId);

                if (workflow == null)
                {
                    _logger.LogWarning(
                        "WORKFLOW_NOT_FOUND_FOR_APPROVAL | ProductId: {ProductId}",
                        message.ProductId);
                    return;
                }
                
                activity?.SetTag("workflow.id", workflow.Id);

                // 🔥 UPDATE STATE: Move to Approved
                workflow.CurrentStep = WorkflowStep.Approved;
                workflow.UpdatedAt = DateTime.UtcNow;

                await repo.UpdateAsync(workflow);

                _logger.LogInformation(
                    "WORKFLOW_APPROVED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | ApprovedBy: {ApprovedBy} | CorrelationId: {CorrelationId}",
                    workflow.Id,
                    workflow.ProductId,
                    message.ApprovedBy,
                    workflow.CorrelationId);

                // 🔥 TRIGGER ORCHESTRATOR: Continue to next step
                await orchestrator.ProcessAsync(workflow.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "WORKFLOW_APPROVAL_PROCESSING_FAILED | ProductId: {ProductId} | Error: {Error}",
                    message.ProductId,
                    ex.Message);
                throw;
            }
        }
    }
}
