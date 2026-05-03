using System.Text.Json;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

// Subscribes to: ecom.domain.events / inventory.updated
public class InventoryUpdatedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.inventory.updated";
    protected override string RoutingKey => "inventory.updated";

    public InventoryUpdatedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<InventoryUpdatedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var eventData = JsonSerializer.Deserialize<InventoryUpdatedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize inventory.updated event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId} for VariantId {VariantId}",
            nameof(InventoryUpdatedEvent),
            eventData.EventId,
            eventData.VariantId);

        await auditService.WriteAsync(new AuditLogDto
        {
            EntityName = "ProductVariant",
            EntityId = eventData.VariantId,
            Action = "StockChanged",
            EventType = "inventory.updated",
            SourceService = "WorkflowService",
            CorrelationId = eventData.CorrelationId ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Processed inventory.updated for VariantId: {VariantId}", 
            eventData.VariantId);
    }

    private class InventoryUpdatedEvent
    {
        public Guid EventId { get; init; }
        public int VariantId { get; set; }
        public int NewQuantity { get; set; }
        public string? CorrelationId { get; set; }
    }
}
