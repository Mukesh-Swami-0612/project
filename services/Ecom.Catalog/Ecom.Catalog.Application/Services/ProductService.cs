using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Ecom.Catalog.Application.Common;
using Ecom.Catalog.Application.CQRS.Commands;
using Ecom.Catalog.Application.CQRS.Handlers;
using Ecom.Catalog.Application.CQRS.Queries;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Exceptions;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Enums;
using Ecom.Catalog.Domain.Events;
using Ecom.Catalog.Domain.Exceptions;
using Ecom.Catalog.Domain.Services;
using Microsoft.AspNetCore.Http;

namespace Ecom.Catalog.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ProductLifecycleService _lifecycleService;
    private readonly ProductValidationService _validationService;
    private readonly OutboxService _outboxService;
    
    // CQRS Handlers
    private readonly CreateProductCommandHandler _createHandler;
    private readonly UpdateProductCommandHandler _updateHandler;
    private readonly GetProductQueryHandler _getQueryHandler;
    private readonly ListProductsQueryHandler _listQueryHandler;

    public ProductService(
        IProductRepository repo, 
        IMapper mapper, 
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        OutboxService outboxService,
        IReadModelRepository readModelRepo)
    {
        _repo = repo;
        _mapper = mapper;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _validationService = new ProductValidationService();
        _lifecycleService = new ProductLifecycleService(_validationService);
        _outboxService = outboxService;
        
        // Initialize CQRS handlers
        _createHandler = new CreateProductCommandHandler(repo, outboxService);
        _updateHandler = new UpdateProductCommandHandler(repo, outboxService);
        _getQueryHandler = new GetProductQueryHandler(readModelRepo, mapper);
        _listQueryHandler = new ListProductsQueryHandler(readModelRepo, mapper);
    }

    // ── QUERY API (CQRS - Reads from Read Model) ─────────────────────────────

    /// <summary>
    /// Query products with structured parameters and pagination
    /// CQRS: Uses ListProductsQueryHandler to read from read model
    /// </summary>
    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryDto query)
    {
        // 🔥 VALIDATE QUERY PARAMETERS
        query.Validate();

        // 🔥 CQRS: Use query handler to read from read model
        var listQuery = new ListProductsQuery
        {
            Search = query.Search,
            CategoryId = query.CategoryId,
            BrandId = query.BrandId,
            Page = query.Page,
            PageSize = query.PageSize,
            SortBy = query.SortBy,
            SortDescending = query.SortOrder == "desc"
        };

        return await _listQueryHandler.HandleAsync(listQuery);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(string? search, int page = 1, int pageSize = 10)
    {
        // 🔥 BACKWARD COMPATIBILITY: Delegate to CQRS query handler
        var listQuery = new ListProductsQuery
        {
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await _listQueryHandler.HandleAsync(listQuery);
        return result.Data;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        // 🔥 CQRS: Use query handler to read from read model
        var query = new GetProductQuery { ProductId = id };
        return await _getQueryHandler.HandleAsync(query);
    }

    // ── COMMAND API (CQRS - Writes to Write Model) ───────────────────────────

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        // 🔥 CQRS: Use command handler to write to write model
        var command = new CreateProductCommand
        {
            Name = dto.Name,
            SKU = dto.SKU,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            CreatedBy = dto.CreatedBy
        };

        var productId = await _createHandler.HandleAsync(command);

        // 🔥 VALIDATION EVENT: Notify workflow that validation passed
        var productValidatedEvent = new ProductValidatedEvent
        {
            ProductId = productId,
            SKU = dto.SKU,
            Name = dto.Name,
            IsValid = true,
            ValidationMessage = "Product validation successful",
            CorrelationId = Guid.NewGuid().ToString("N"),
            OccurredAt = DateTime.UtcNow
        };
        await _outboxService.AddEventAsync(productValidatedEvent);
        await _repo.SaveChangesAsync();

        // Audit log
        var user = GetCurrentUser();
        await _auditService.LogAsync(
            "Product", 
            productId, 
            "Created", 
            JsonSerializer.Serialize(dto),
            user.userId,
            user.email,
            GetClientIp());

        // Return DTO by reading from write model (eventual consistency with read model)
        var product = await _repo.GetByIdAsync(productId);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");

        var oldValues = new { product.Name, product.CategoryId, product.BrandId };

        // 🔥 CQRS: Use command handler to write to write model
        var command = new UpdateProductCommand
        {
            Id = id,
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            RowVersion = dto.RowVersion
        };

        await _updateHandler.HandleAsync(command);

        // Audit log
        var user = GetCurrentUser();
        var changes = JsonSerializer.Serialize(new { Old = oldValues, New = dto });
        await _auditService.LogAsync(
            "Product",
            id,
            "Updated",
            changes,
            user.userId,
            user.email,
            GetClientIp());

        // Return DTO by reading from write model (eventual consistency with read model)
        product = await _repo.GetByIdAsync(id);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");
        
        // 🔥 DOMAIN RULE: Check if product can be deleted
        if (!product.IsDeletable())
        {
            throw new InvalidOperationException(
                $"Product cannot be deleted in {product.GetLifecycleStatus()} status. " +
                $"Only Draft, Rejected, and Archived products can be deleted.");
        }

        await _repo.DeleteAsync(id);

        // Audit log
        var user = GetCurrentUser();
        await _auditService.LogAsync(
            "Product",
            id,
            "Deleted",
            null,
            user.userId,
            user.email,
            GetClientIp());
    }

    // ── LIFECYCLE MANAGEMENT ─────────────────────────────────────────────────────

    public async Task<ProductStatusDto> GetProductStatusAsync(int productId)
    {
        var product = await _repo.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product {productId} not found.");

        var currentStatus = product.GetLifecycleStatus();
        var validNextStates = _lifecycleService.GetValidNextStates(currentStatus);

        return new ProductStatusDto
        {
            StatusId = product.StatusId,
            StatusName = currentStatus.ToString(),
            ValidNextStates = validNextStates.Select(s => s.ToString()).ToList(),
            CanEdit = product.IsEditable(),
            CanDelete = product.IsDeletable()
        };
    }

    public async Task<ProductDto> TransitionStatusAsync(int productId, TransitionProductStatusDto dto)
    {
        var product = await _repo.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product {productId} not found.");

        var oldStatus = product.GetLifecycleStatus();

        // 🔥 DOMAIN SERVICE: Enforce state transition rules
        _lifecycleService.TransitionTo(product, dto.TargetStatus, dto.Reason ?? "");

        // 🔥 CONCURRENCY CONTROL: Repository will throw ConcurrencyException on conflict
        await _repo.UpdateAsync(product);

        // 🔥 OUTBOX PATTERN: Add event to outbox in same transaction
        var statusChangedEvent = new ProductStatusChangedEvent
        {
            ProductId = productId,
            SKU = product.SKU,
            FromStatus = oldStatus.ToString(),
            ToStatus = dto.TargetStatus.ToString(),
            Reason = dto.Reason,
            ChangedBy = GetCurrentUser().userId,
            OccurredAt = DateTime.UtcNow
        };
        await _outboxService.AddEventAsync(statusChangedEvent);

        // 🔥 SPECIAL EVENT: Product approved (critical for workflow and notifications)
        if (dto.TargetStatus == ProductLifecycleStatus.Approved)
        {
            var approvedEvent = new ProductApprovedEvent
            {
                ProductId = productId,
                SKU = product.SKU,
                Name = product.Name,
                ApprovedBy = GetCurrentUser().userId,
                Comments = dto.Reason,
                CorrelationId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTime.UtcNow
            };
            await _outboxService.AddEventAsync(approvedEvent);
        }

        // 🔥 SPECIAL EVENT: Product rejected (critical for workflow and notifications)
        if (dto.TargetStatus == ProductLifecycleStatus.Rejected)
        {
            var rejectedEvent = new ProductRejectedEvent
            {
                ProductId = productId,
                SKU = product.SKU,
                Name = product.Name,
                RejectedBy = GetCurrentUser().userId,
                Reason = dto.Reason ?? "No reason provided",
                CorrelationId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTime.UtcNow
            };
            await _outboxService.AddEventAsync(rejectedEvent);
        }

        // 🔥 SPECIAL EVENT: Product published (critical for other services)
        if (dto.TargetStatus == ProductLifecycleStatus.Published)
        {
            var publishedEvent = new ProductPublishedEvent
            {
                ProductId = productId,
                SKU = product.SKU,
                Name = product.Name,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                PublishedBy = GetCurrentUser().userId,
                OccurredAt = DateTime.UtcNow
            };
            await _outboxService.AddEventAsync(publishedEvent);
        }

        // Audit log
        var user = GetCurrentUser();
        await _auditService.LogAsync(
            "Product",
            productId,
            "StatusTransition",
            JsonSerializer.Serialize(new { From = oldStatus.ToString(), To = dto.TargetStatus.ToString(), Reason = dto.Reason }),
            user.userId,
            user.email,
            GetClientIp());

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> SubmitForReviewAsync(int productId)
    {
        return await TransitionStatusAsync(productId, new TransitionProductStatusDto
        {
            TargetStatus = ProductLifecycleStatus.ReadyForReview,
            Reason = "Submitted for review"
        });
    }

    public async Task<ProductDto> ApproveAsync(int productId)
    {
        return await TransitionStatusAsync(productId, new TransitionProductStatusDto
        {
            TargetStatus = ProductLifecycleStatus.Approved,
            Reason = "Approved by reviewer"
        });
    }

    public async Task<ProductDto> RejectAsync(int productId, string reason)
    {
        return await TransitionStatusAsync(productId, new TransitionProductStatusDto
        {
            TargetStatus = ProductLifecycleStatus.Rejected,
            Reason = reason
        });
    }

    public async Task<ProductDto> PublishAsync(int productId)
    {
        return await TransitionStatusAsync(productId, new TransitionProductStatusDto
        {
            TargetStatus = ProductLifecycleStatus.Published,
            Reason = "Published to storefront"
        });
    }

    public async Task<ProductDto> ArchiveAsync(int productId)
    {
        return await TransitionStatusAsync(productId, new TransitionProductStatusDto
        {
            TargetStatus = ProductLifecycleStatus.Archived,
            Reason = "Archived"
        });
    }

    private (int userId, string email) GetCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = user.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@system";
            return (int.TryParse(userIdClaim, out var uid) ? uid : 0, emailClaim);
        }
        return (0, "system@internal");
    }

    private string? GetClientIp()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
