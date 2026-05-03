using Ecom.Workflow.Application.Events;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Telemetry;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecom.Workflow.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes product.published events from Catalog service
/// Updates workflow to Published and triggers completion
/// </summary>
public class ProductPublishedConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductPublishedConsumer> _logger;

    public ProductPublishedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductPublishedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ProductPublishedEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = WorkflowActivitySource.Instance.StartActivity(
                "ProcessProductPublished",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = WorkflowActivitySource.Instance.StartActivity("ProcessProductPublished");
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
                nameof(ProductPublishedEvent),
                message?.EventId);

            _logger.LogInformation(
                "WORKFLOW_RECEIVED_PRODUCT_PUBLISHED | ProductId: {ProductId} | SKU: {SKU} | CorrelationId: {CorrelationId}",
                message.ProductId,
                message.SKU,
                message.CorrelationId);

            try
            {
                var workflow = await repo.GetByProductIdAsync(message.ProductId);

                if (workflow == null)
                {
                    _logger.LogWarning(
                        "WORKFLOW_NOT_FOUND_FOR_PUBLISH | ProductId: {ProductId}",
                        message.ProductId);
                    return;
                }
                
                activity?.SetTag("workflow.id", workflow.Id);

                // 🔥 UPDATE STATE: Move to Published
                workflow.CurrentStep = WorkflowStep.Published;
                workflow.UpdatedAt = DateTime.UtcNow;

                await repo.UpdateAsync(workflow);

                _logger.LogInformation(
                    "WORKFLOW_PUBLISHED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | SKU: {SKU} | CorrelationId: {CorrelationId}",
                    workflow.Id,
                    workflow.ProductId,
                    message.SKU,
                    workflow.CorrelationId);

                // 🔥 TRIGGER ORCHESTRATOR: Complete workflow
                await orchestrator.ProcessAsync(workflow.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "WORKFLOW_PUBLISH_PROCESSING_FAILED | ProductId: {ProductId} | Error: {Error}",
                    message.ProductId,
                    ex.Message);
                throw;
            }
        }
    }
}
