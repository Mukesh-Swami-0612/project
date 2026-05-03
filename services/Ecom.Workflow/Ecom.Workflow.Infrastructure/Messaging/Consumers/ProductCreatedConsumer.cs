using Ecom.Workflow.Application.Events;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Telemetry;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecom.Workflow.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes product.created events from Catalog service
/// Creates WorkflowInstance to begin orchestration
/// Implements idempotency to handle duplicate events safely
/// </summary>
public class ProductCreatedConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ProductCreatedEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = WorkflowActivitySource.Instance.StartActivity(
                "ProcessProductCreated",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = WorkflowActivitySource.Instance.StartActivity("ProcessProductCreated");
        }

        using (activity)
        {
            activity?.SetTag("event.id", message?.EventId);
            activity?.SetTag("product.id", message?.ProductId);

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
            var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

            // Generate correlation ID for tracing
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation(
                "Processing event {EventType} with EventId {EventId}",
                nameof(ProductCreatedEvent),
                message?.EventId);

            // Phase 1: Check idempotency (tracking only, no blocking)
            var alreadyProcessed = await idempotencyService.IsProcessedAsync(message.EventId);
            _logger.LogInformation(
                "Idempotency check for EventId {EventId}: {Result}",
                message.EventId,
                alreadyProcessed ? "ALREADY_PROCESSED" : "NEW");

            _logger.LogInformation(
                "WORKFLOW_RECEIVED_PRODUCT_CREATED | ProductId: {ProductId} | SKU: {SKU} | CorrelationId: {CorrelationId}",
                message.ProductId,
                message.SKU,
                correlationId);

            try
            {
                // 🔥 IDEMPOTENCY CHECK: Prevent duplicate workflow creation
                var existing = await repo.GetByProductIdAsync(message.ProductId);

                if (existing != null)
                {
                    _logger.LogWarning(
                        "WORKFLOW_ALREADY_EXISTS | ProductId: {ProductId} | WorkflowId: {WorkflowId} | Status: {Status} | Step: {Step}",
                        message.ProductId,
                        existing.Id,
                        existing.Status,
                        existing.CurrentStep);
                    return;
                }

                // 🔥 CREATE WORKFLOW INSTANCE: Begin orchestration
                var workflow = new WorkflowInstance
                {
                    Id = Guid.NewGuid(),
                    ProductId = message.ProductId,
                    Status = WorkflowStatus.InProgress,
                    CurrentStep = WorkflowStep.Created,
                    RetryCount = 0,
                    CorrelationId = correlationId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await repo.AddAsync(workflow);
                
                activity?.SetTag("workflow.id", workflow.Id);

                _logger.LogInformation(
                    "WORKFLOW_CREATED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | SKU: {SKU} | Status: {Status} | Step: {Step} | CorrelationId: {CorrelationId}",
                    workflow.Id,
                    workflow.ProductId,
                    message.SKU,
                    workflow.Status,
                    workflow.CurrentStep,
                    workflow.CorrelationId);

                // 🔥 TRIGGER ORCHESTRATOR: Begin workflow processing
                var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();
                await orchestrator.ProcessAsync(workflow.Id);

                // Phase 1: Mark as processed after successful processing
                await idempotencyService.MarkProcessedAsync(message.EventId, nameof(ProductCreatedEvent));
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "WORKFLOW_CREATION_FAILED | ProductId: {ProductId} | SKU: {SKU} | Error: {Error}",
                    message.ProductId,
                    message.SKU,
                    ex.Message);
                throw;
            }
        }
    }
}
