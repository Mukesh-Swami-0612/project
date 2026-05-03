using Ecom.Catalog.Application.Common;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Domain.Enums;

namespace Ecom.Catalog.Application.Interfaces;

public interface IProductService
{
    // 🔥 QUERY API: Structured query with pagination
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryDto query);
    
    // Legacy method (kept for backward compatibility)
    Task<IEnumerable<ProductDto>> GetAllAsync(string? search, int page = 1, int pageSize = 10);
    
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto);
    Task DeleteAsync(int id);
    
    // 🔥 LIFECYCLE MANAGEMENT
    Task<ProductStatusDto> GetProductStatusAsync(int productId);
    Task<ProductDto> TransitionStatusAsync(int productId, TransitionProductStatusDto dto);
    Task<ProductDto> SubmitForReviewAsync(int productId);
    Task<ProductDto> ApproveAsync(int productId);
    Task<ProductDto> RejectAsync(int productId, string reason);
    Task<ProductDto> PublishAsync(int productId);
    Task<ProductDto> ArchiveAsync(int productId);
}
