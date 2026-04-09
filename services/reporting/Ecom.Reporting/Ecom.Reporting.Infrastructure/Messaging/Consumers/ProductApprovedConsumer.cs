using System.Text.Json;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

// Subscribes to: ecom.domain.events / product.approved
public class ProductApprovedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.product.approved";
    protected override string RoutingKey => "product.approved";

    public ProductApprovedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<ProductApprovedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var eventData = JsonSerializer.Deserialize<ProductApprovedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize product.approved event");
            return;
        }

        await auditService.WriteAsync(new AuditLogDto
        {
            EntityName = "Product",
            EntityId = eventData.ProductId,
            Action = "Approved",
            EventType = "product.approved",
            SourceService = "WorkflowService",
            CorrelationId = eventData.CorrelationId ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Processed product.approved for ProductId: {ProductId}", 
            eventData.ProductId);
    }

    private class ProductApprovedEvent
    {
        public int ProductId { get; set; }
        public int ApprovedBy { get; set; }
        public string? CorrelationId { get; set; }
    }
}
