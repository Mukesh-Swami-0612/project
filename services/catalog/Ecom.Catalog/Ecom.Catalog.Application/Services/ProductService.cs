using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Ecom.Catalog.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductService(
        IProductRepository repo, 
        IMapper mapper, 
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _mapper = mapper;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(string? search, int page = 1, int pageSize = 10)
    {
        var products = await _repo.GetAllAsync(search, page, pageSize);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        if (await _repo.SkuExistsAsync(dto.SKU))
            throw new InvalidOperationException($"SKU '{dto.SKU}' already exists.");

        var product = _mapper.Map<Product>(dto);
        await _repo.AddAsync(product);

        // Audit log
        var user = GetCurrentUser();
        await _auditService.LogAsync(
            "Product", 
            product.Id, 
            "Created", 
            JsonSerializer.Serialize(dto),
            user.userId,
            user.email,
            GetClientIp());

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");

        var oldValues = new { product.Name, product.CategoryId, product.BrandId };
        
        product.Name = dto.Name;
        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product);

        // Audit log
        var user = GetCurrentUser();
        var changes = JsonSerializer.Serialize(new { Old = oldValues, New = dto });
        await _auditService.LogAsync(
            "Product",
            product.Id,
            "Updated",
            changes,
            user.userId,
            user.email,
            GetClientIp());

        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");
        
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
