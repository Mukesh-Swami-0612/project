using System.Text.Json;
using Ecom.Reporting.Domain.Entities;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

public class WorkflowFailedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.workflow.failed";
    protected override string RoutingKey => "workflow.failed";

    public WorkflowFailedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<WorkflowFailedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var eventData = JsonSerializer.Deserialize<WorkflowFailedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize workflow.failed event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId}",
            nameof(WorkflowFailedEvent),
            eventData?.EventId);

        var workflow = await context.Workflows
            .FindAsync(eventData.WorkflowId);

        if (workflow == null)
        {
            workflow = new WorkflowReadModel
            {
                WorkflowId = eventData.WorkflowId,
                Status = "Failed",
                WorkflowType = eventData.WorkflowType ?? "Unknown",
                EntityId = eventData.EntityId,
                FailureReason = eventData.FailureReason,
                RetryCount = eventData.RetryCount,
                CreatedAt = eventData.CreatedAt
            };
            context.Workflows.Add(workflow);
        }
        else
        {
            workflow.Status = "Failed";
            workflow.FailureReason = eventData.FailureReason;
            workflow.RetryCount = eventData.RetryCount;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Processed workflow.failed for WorkflowId: {WorkflowId}", 
            eventData.WorkflowId);
    }

    private class WorkflowFailedEvent
    {
        public Guid EventId { get; init; }
        public string WorkflowId { get; set; } = string.Empty;
        public string? WorkflowType { get; set; }
        public int EntityId { get; set; }
        public string? FailureReason { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
