using System.Text.Json;
using Ecom.Reporting.Domain.Entities;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

public class ProductStatusChangedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.product.status.changed";
    protected override string RoutingKey => "product.status.changed";

    public ProductStatusChangedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<ProductStatusChangedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var eventData = JsonSerializer.Deserialize<ProductStatusChangedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize product status changed event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId}",
            nameof(ProductStatusChangedEvent),
            eventData?.EventId);

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.ProductId == eventData.ProductId);

        if (product == null)
        {
            product = new ProductReadModel
            {
                ProductId = eventData.ProductId,
                Status = eventData.Status,
                Name = eventData.Name ?? "Unknown",
                StockQuantity = eventData.StockQuantity,
                IsLowStock = eventData.StockQuantity < 10,
                CreatedAt = DateTime.UtcNow,
                PublishedAt = eventData.Status == "Published" ? DateTime.UtcNow : null
            };
            context.Products.Add(product);
        }
        else
        {
            product.Status = eventData.Status;
            product.Name = eventData.Name ?? product.Name;
            product.StockQuantity = eventData.StockQuantity;
            product.IsLowStock = eventData.StockQuantity < 10;
            if (eventData.Status == "Published" && product.PublishedAt == null)
                product.PublishedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Processed product status changed for ProductId: {ProductId}", 
            eventData.ProductId);
    }

    private class ProductStatusChangedEvent
    {
        public Guid EventId { get; init; }
        public int ProductId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int StockQuantity { get; set; }
    }
}
