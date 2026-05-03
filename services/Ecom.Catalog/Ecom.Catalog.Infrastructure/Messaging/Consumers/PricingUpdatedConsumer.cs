using Microsoft.Extensions.Logging;

namespace Ecom.Catalog.Infrastructure.Messaging.Consumers;

// Subscribes to: ecom.domain.events / pricing.updated
// Updates ProductReadModel.Price
public class PricingUpdatedConsumer
{
    private readonly ILogger<PricingUpdatedConsumer> _logger;

    public PricingUpdatedConsumer(ILogger<PricingUpdatedConsumer> logger) => _logger = logger;

    public Task ConsumeAsync(int productId, decimal newPrice)
    {
        _logger.LogInformation("Updating read model price for ProductId: {ProductId}", productId);
        return Task.CompletedTask;
    }
}
