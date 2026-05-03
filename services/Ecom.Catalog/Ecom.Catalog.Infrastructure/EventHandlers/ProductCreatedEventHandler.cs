using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ecom.Catalog.Infrastructure.EventHandlers;

/// <summary>
/// Handles ProductCreatedEvent to update read model
/// Ensures eventual consistency between write and read models
/// </summary>
public class ProductCreatedEventHandler
{
    private readonly IReadModelRepository _readModelRepo;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(
        IReadModelRepository readModelRepo,
        ILogger<ProductCreatedEventHandler> logger)
    {
        _readModelRepo = readModelRepo;
        _logger = logger;
    }

    public async Task HandleAsync(ProductCreatedEvent @event)
    {
        _logger.LogInformation(
            "Handling ProductCreatedEvent for ProductId: {ProductId}",
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
