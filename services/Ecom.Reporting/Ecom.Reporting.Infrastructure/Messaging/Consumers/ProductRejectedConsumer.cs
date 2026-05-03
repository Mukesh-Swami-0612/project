using System.Text.Json;
using Ecom.Reporting.Domain.Entities;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

public class ProductRejectedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.product.rejected";
    protected override string RoutingKey => "product.rejected";

    public ProductRejectedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<ProductRejectedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var eventData = JsonSerializer.Deserialize<ProductRejectedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize product.rejected event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId}",
            nameof(ProductRejectedEvent),
            eventData?.EventId);

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.ProductId == eventData.ProductId);

        if (product == null)
        {
            product = new ProductReadModel
            {
                ProductId = eventData.ProductId,
                Status = "Rejected",
                Name = eventData.Name ?? "Unknown",
                StockQuantity = 0,
                IsLowStock = false,
                RejectionReason = eventData.RejectionReason,
                CreatedAt = DateTime.UtcNow
            };
            context.Products.Add(product);
        }
        else
        {
            product.Status = "Rejected";
            product.RejectionReason = eventData.RejectionReason;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Processed product.rejected for ProductId: {ProductId}", 
            eventData.ProductId);
    }

    private class ProductRejectedEvent
    {
        public Guid EventId { get; init; }
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? RejectionReason { get; set; }
    }
}
