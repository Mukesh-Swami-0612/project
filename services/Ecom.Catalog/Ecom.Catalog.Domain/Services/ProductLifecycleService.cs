using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Enums;

namespace Ecom.Catalog.Domain.Services;

/// <summary>
/// Domain service that enforces product lifecycle state transitions
/// This is the SINGLE SOURCE OF TRUTH for product state management
/// </summary>
public class ProductLifecycleService
{
    private readonly ProductValidationService _validationService;

    public ProductLifecycleService(ProductValidationService validationService)
    {
        _validationService = validationService;
    }

    public ProductLifecycleService()
    {
        _validationService = new ProductValidationService();
    }
    // 🔥 DOMAIN RULE: Valid state transitions
    private static readonly Dictionary<ProductLifecycleStatus, List<ProductLifecycleStatus>> ValidTransitions = new()
    {
        {
            ProductLifecycleStatus.Draft, new List<ProductLifecycleStatus>
            {
                ProductLifecycleStatus.InEnrichment,
                ProductLifecycleStatus.Archived
            }
        },
        {
            ProductLifecycleStatus.InEnrichment, new List<ProductLifecycleStatus>
            {
                ProductLifecycleStatus.ReadyForReview,
                ProductLifecycleStatus.Draft,
                ProductLifecycleStatus.Archived
            }
        },
        {
            ProductLifecycleStatus.ReadyForReview, new List<ProductLifecycleStatus>
            {
                ProductLifecycleStatus.Approved,
                ProductLifecycleStatus.Rejected,
                ProductLifecycleStatus.InEnrichment
            }
        },
        {
            ProductLifecycleStatus.Approved, new List<ProductLifecycleStatus>
            {
                ProductLifecycleStatus.Published,
                ProductLifecycleStatus.Archived
            }
        },
        {
            ProductLifecycleStatus.Published, new List<ProductLifecycleStatus>
            {
                ProductLifecycleStatus.Archived
            }
        },
        {
            ProductLifecycleStatus.Rejected, new List<ProductLifecycleStatus>
            {
                ProductLifecycleStatus.InEnrichment,
                ProductLifecycleStatus.Archived
            }
        },
        {
            ProductLifecycleStatus.Archived, new List<ProductLifecycleStatus>()
        }
    };

    /// <summary>
    /// Validates if a state transition is allowed
    /// </summary>
    public bool CanTransitionTo(ProductLifecycleStatus currentStatus, ProductLifecycleStatus targetStatus)
    {
        if (!ValidTransitions.ContainsKey(currentStatus))
            return false;

        return ValidTransitions[currentStatus].Contains(targetStatus);
    }

    /// <summary>
    /// Attempts to transition product to new status
    /// Throws exception if transition is invalid
    /// </summary>
    public void TransitionTo(Product product, ProductLifecycleStatus targetStatus, string reason = "")
    {
        var currentStatus = (ProductLifecycleStatus)product.StatusId;

        if (currentStatus == targetStatus)
            throw new InvalidOperationException($"Product is already in {targetStatus} status.");

        if (!CanTransitionTo(currentStatus, targetStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition product from {currentStatus} to {targetStatus}. " +
                $"Valid transitions from {currentStatus}: {string.Join(", ", ValidTransitions[currentStatus])}");
        }

        // 🔥 DOMAIN RULE: Validate business rules before transition
        ValidateTransitionRules(product, currentStatus, targetStatus);

        // 🔥 VALIDATION HOOKS: Ensure data correctness for specific transitions
        switch (targetStatus)
        {
            case ProductLifecycleStatus.ReadyForReview:
                _validationService.ValidateForReview(product);
                break;

            case ProductLifecycleStatus.Published:
                _validationService.ValidateForPublish(product);
                break;

            case ProductLifecycleStatus.Archived:
                _validationService.ValidateForArchive(product);
                break;
        }

        // Apply transition
        product.StatusId = (int)targetStatus;
        product.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates business rules for specific transitions
    /// </summary>
    private void ValidateTransitionRules(Product product, ProductLifecycleStatus from, ProductLifecycleStatus to)
    {
        switch (to)
        {
            case ProductLifecycleStatus.ReadyForReview:
                // 🔥 RULE: Product must have basic info before review
                if (string.IsNullOrWhiteSpace(product.Name))
                    throw new InvalidOperationException("Product must have a name before review.");
                if (string.IsNullOrWhiteSpace(product.SKU))
                    throw new InvalidOperationException("Product must have a SKU before review.");
                if (product.CategoryId <= 0)
                    throw new InvalidOperationException("Product must have a category before review.");
                break;

            case ProductLifecycleStatus.Approved:
                // 🔥 RULE: Only from ReadyForReview
                if (from != ProductLifecycleStatus.ReadyForReview)
                    throw new InvalidOperationException("Only products in ReadyForReview can be approved.");
                break;

            case ProductLifecycleStatus.Published:
                // 🔥 RULE: Must be approved first
                if (from != ProductLifecycleStatus.Approved)
                    throw new InvalidOperationException("Only approved products can be published.");
                break;

            case ProductLifecycleStatus.Rejected:
                // 🔥 RULE: Only from ReadyForReview
                if (from != ProductLifecycleStatus.ReadyForReview)
                    throw new InvalidOperationException("Only products in ReadyForReview can be rejected.");
                break;
        }
    }

    /// <summary>
    /// Gets all valid next states for current status
    /// </summary>
    public List<ProductLifecycleStatus> GetValidNextStates(ProductLifecycleStatus currentStatus)
    {
        return ValidTransitions.ContainsKey(currentStatus)
            ? ValidTransitions[currentStatus]
            : new List<ProductLifecycleStatus>();
    }

    /// <summary>
    /// Checks if product can be edited in current status
    /// </summary>
    public bool CanEdit(ProductLifecycleStatus status)
    {
        return status switch
        {
            ProductLifecycleStatus.Draft => true,
            ProductLifecycleStatus.InEnrichment => true,
            ProductLifecycleStatus.Rejected => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if product can be deleted in current status
    /// </summary>
    public bool CanDelete(ProductLifecycleStatus status)
    {
        return status switch
        {
            ProductLifecycleStatus.Draft => true,
            ProductLifecycleStatus.Rejected => true,
            ProductLifecycleStatus.Archived => true,
            _ => false
        };
    }
}
