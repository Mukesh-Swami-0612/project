using System.Text.Json;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

// Subscribes to: ecom.domain.events / product.published
public class ProductPublishedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.product.published";
    protected override string RoutingKey => "product.published";

    public ProductPublishedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<ProductPublishedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var eventData = JsonSerializer.Deserialize<ProductPublishedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize product.published event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId} for ProductId {ProductId}",
            nameof(ProductPublishedEvent),
            eventData.EventId,
            eventData.ProductId);

        await auditService.WriteAsync(new AuditLogDto
        {
            EntityName = "Product",
            EntityId = eventData.ProductId,
            Action = "Published",
            EventType = "product.published",
            SourceService = "WorkflowService",
            CorrelationId = eventData.CorrelationId ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Processed product.published for ProductId: {ProductId}", 
            eventData.ProductId);
    }

    private class ProductPublishedEvent
    {
        public Guid EventId { get; init; }
        public int ProductId { get; set; }
        public int PublishedBy { get; set; }
        public string? CorrelationId { get; set; }
    }
}
