using Ecom.Catalog.Application.CQRS.Commands;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Application.Services;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Enums;
using Ecom.Catalog.Domain.Events;
using Ecom.Catalog.Domain.Exceptions;
using Ecom.Catalog.Domain.Services;

namespace Ecom.Catalog.Application.CQRS.Handlers;

/// <summary>
/// Handles CreateProductCommand - writes to write model
/// </summary>
public class CreateProductCommandHandler
{
    private readonly IProductRepository _repo;
    private readonly OutboxService _outboxService;
    private readonly ProductValidationService _validationService;

    public CreateProductCommandHandler(
        IProductRepository repo,
        OutboxService outboxService)
    {
        _repo = repo;
        _outboxService = outboxService;
        _validationService = new ProductValidationService();
    }

    public async Task<int> HandleAsync(CreateProductCommand command)
    {
        // Create product entity
        var product = new Product
        {
            Name = command.Name,
            SKU = command.SKU,
            CategoryId = command.CategoryId,
            BrandId = command.BrandId,
            StatusId = (int)ProductLifecycleStatus.Draft,
            CreatedBy = command.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        // Domain validation
        _validationService.ValidateForCreation(product);

        // SKU uniqueness check
        if (await _repo.SkuExistsAsync(command.SKU))
            throw new DomainException($"SKU '{command.SKU}' already exists.", "SKU");

        // Save to write model
        await _repo.AddAsync(product);

        // Publish event via outbox
        var productCreatedEvent = new ProductCreatedEvent
        {
            ProductId = product.Id,
            SKU = product.SKU,
            CreatedBy = product.CreatedBy,
            OccurredAt = DateTime.UtcNow
        };

        await _outboxService.AddEventAsync(productCreatedEvent);
        await _repo.SaveChangesAsync();

        return product.Id;
    }
}
