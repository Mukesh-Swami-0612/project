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
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.StatusName));

        CreateMap<CreateProductDto, Product>();
        CreateMap<Category, CategoryDto>();
        CreateMap<Brand, BrandDto>();
        CreateMap<MediaAsset, MediaAssetDto>();
        CreateMap<ProductVariantCombination, ProductVariantDto>();
        CreateMap<ProductReadModel, ProductReadModelDto>();
    }
}
