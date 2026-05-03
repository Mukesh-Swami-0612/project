using System.Text.Json;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

// Subscribes to: ecom.domain.events / pricing.updated
public class PricingUpdatedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.pricing.updated";
    protected override string RoutingKey => "pricing.updated";

    public PricingUpdatedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<PricingUpdatedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var eventData = JsonSerializer.Deserialize<PricingUpdatedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize pricing.updated event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId} for VariantId {VariantId}",
            nameof(PricingUpdatedEvent),
            eventData.EventId,
            eventData.VariantId);

        await auditService.WriteAsync(new AuditLogDto
        {
            EntityName = "ProductVariant",
            EntityId = eventData.VariantId,
            Action = "PriceChanged",
            EventType = "pricing.updated",
            SourceService = "WorkflowService",
            CorrelationId = eventData.CorrelationId ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Processed pricing.updated for VariantId: {VariantId}", 
            eventData.VariantId);
    }

    private class PricingUpdatedEvent
    {
        public Guid EventId { get; init; }
        public int VariantId { get; set; }
        public decimal NewPrice { get; set; }
        public string? CorrelationId { get; set; }
    }
}
