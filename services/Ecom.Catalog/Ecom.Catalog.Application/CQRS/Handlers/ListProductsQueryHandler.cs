using AutoMapper;
using Ecom.Catalog.Application.Common;
using Ecom.Catalog.Application.CQRS.Queries;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;

namespace Ecom.Catalog.Application.CQRS.Handlers;

/// <summary>
/// Handles ListProductsQuery - reads from read model
/// </summary>
public class ListProductsQueryHandler
{
    private readonly IReadModelRepository _readModelRepo;
    private readonly IMapper _mapper;

    public ListProductsQueryHandler(
        IReadModelRepository readModelRepo,
        IMapper mapper)
    {
        _readModelRepo = readModelRepo;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> HandleAsync(ListProductsQuery query)
    {
        // Read from optimized read model
        var (items, totalCount) = await _readModelRepo.QueryAsync(
            search: query.Search,
            categoryId: query.CategoryId,
            brandId: query.BrandId,
            statusId: query.StatusId,
            page: query.Page,
            pageSize: query.PageSize,
            sortBy: query.SortBy,
            sortDescending: query.SortDescending
        );

        var dtoList = _mapper.Map<List<ProductDto>>(items);

        return PagedResult<ProductDto>.Create(
            dtoList,
            query.Page,
            query.PageSize,
            totalCount
        );
    }
}
