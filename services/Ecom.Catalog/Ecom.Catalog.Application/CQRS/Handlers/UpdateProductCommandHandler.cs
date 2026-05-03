using Ecom.Catalog.Application.CQRS.Commands;
using Ecom.Catalog.Application.Exceptions;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Application.Services;
using Ecom.Catalog.Domain.Events;
using Ecom.Catalog.Domain.Services;

namespace Ecom.Catalog.Application.CQRS.Handlers;

/// <summary>
/// Handles UpdateProductCommand - writes to write model
/// </summary>
public class UpdateProductCommandHandler
{
    private readonly IProductRepository _repo;
    private readonly OutboxService _outboxService;
    private readonly ProductValidationService _validationService;

    public UpdateProductCommandHandler(
        IProductRepository repo,
        OutboxService outboxService)
    {
        _repo = repo;
        _outboxService = outboxService;
        _validationService = new ProductValidationService();
    }

    public async Task HandleAsync(UpdateProductCommand command)
    {
        var product = await _repo.GetByIdAsync(command.Id);
        if (product == null)
            throw new KeyNotFoundException($"Product {command.Id} not found.");

        // Check if product is editable
        if (!product.IsEditable())
            throw new InvalidOperationException($"Product in {product.GetLifecycleStatus()} status cannot be edited.");

        // Update properties
        product.Name = command.Name;
        product.CategoryId = command.CategoryId;
        product.BrandId = command.BrandId;
        product.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(command.RowVersion))
        {
            product.RowVersion = Convert.FromBase64String(command.RowVersion);
        }

        // Domain validation
        _validationService.ValidateForUpdate(product);

        // Save to write model
        await _repo.UpdateAsync(product);

        // Publish event via outbox
        var productUpdatedEvent = new ProductUpdatedEvent
        {
            ProductId = product.Id,
            SKU = product.SKU,
            OccurredAt = DateTime.UtcNow
        };

        await _outboxService.AddEventAsync(productUpdatedEvent);
        await _repo.SaveChangesAsync();
    }
}
