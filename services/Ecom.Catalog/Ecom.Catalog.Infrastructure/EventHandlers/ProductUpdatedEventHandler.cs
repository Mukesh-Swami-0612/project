using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ecom.Catalog.Infrastructure.EventHandlers;

/// <summary>
/// Handles ProductUpdatedEvent to update read model
/// Ensures eventual consistency between write and read models
/// </summary>
public class ProductUpdatedEventHandler
{
    private readonly IReadModelRepository _readModelRepo;
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(
        IReadModelRepository readModelRepo,
        ILogger<ProductUpdatedEventHandler> logger)
    {
        _readModelRepo = readModelRepo;
        _logger = logger;
    }

    public async Task HandleAsync(ProductUpdatedEvent @event)
    {
        _logger.LogInformation(
            "Handling ProductUpdatedEvent for ProductId: {ProductId}",
            @event.ProductId);

        try
        {
            // Sync read model from write model
            await _readModelRepo.SyncFromProductAsync(@event.ProductId);

            _logger.LogInformation(
                "Read model updated for ProductId: {ProductId}",
                @event.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update read model for ProductId: {ProductId}",
                @event.ProductId);
            throw;
        }
    }
}
