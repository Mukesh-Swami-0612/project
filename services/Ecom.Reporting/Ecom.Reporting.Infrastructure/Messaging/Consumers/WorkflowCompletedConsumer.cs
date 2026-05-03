using System.Text.Json;
using Ecom.Reporting.Domain.Entities;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

public class WorkflowCompletedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.workflow.completed";
    protected override string RoutingKey => "workflow.completed";

    public WorkflowCompletedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<WorkflowCompletedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var eventData = JsonSerializer.Deserialize<WorkflowCompletedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize workflow.completed event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId}",
            nameof(WorkflowCompletedEvent),
            eventData?.EventId);

        var workflow = await context.Workflows
            .FindAsync(eventData.WorkflowId);

        if (workflow == null)
        {
            workflow = new WorkflowReadModel
            {
                WorkflowId = eventData.WorkflowId,
                Status = "Completed",
                WorkflowType = eventData.WorkflowType ?? "Unknown",
                EntityId = eventData.EntityId,
                RetryCount = eventData.RetryCount,
                CreatedAt = eventData.CreatedAt,
                CompletedAt = DateTime.UtcNow
            };
            context.Workflows.Add(workflow);
        }
        else
        {
            workflow.Status = "Completed";
            workflow.RetryCount = eventData.RetryCount;
            workflow.CompletedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Processed workflow.completed for WorkflowId: {WorkflowId}", 
            eventData.WorkflowId);
    }

    private class WorkflowCompletedEvent
    {
        public Guid EventId { get; init; }
        public string WorkflowId { get; set; } = string.Empty;
        public string? WorkflowType { get; set; }
        public int EntityId { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
