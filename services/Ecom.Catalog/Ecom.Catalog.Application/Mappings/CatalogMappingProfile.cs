using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Mappings;

public class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.Name)) // Fixed: Use Name instead of StatusName
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => s.RowVersion != null ? Convert.ToBase64String(s.RowVersion) : null));

        CreateMap<CreateProductDto, Product>();
        
        CreateMap<UpdateProductDto, Product>()
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => s.RowVersion != null ? Convert.FromBase64String(s.RowVersion) : null));
        
        CreateMap<Category, CategoryDto>();
        CreateMap<Brand, BrandDto>();
        CreateMap<MediaAsset, MediaAssetDto>();
        CreateMap<ProductVariantCombination, ProductVariantDto>();
        CreateMap<ProductReadModel, ProductReadModelDto>();
        
        CreateMap<ProductStatus, ProductStatusDto>()
            .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Name));
    }
}
