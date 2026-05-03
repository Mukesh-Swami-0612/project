using AutoMapper;
using Ecom.Catalog.Application.CQRS.Queries;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;

namespace Ecom.Catalog.Application.CQRS.Handlers;

/// <summary>
/// Handles GetProductQuery - reads from read model
/// </summary>
public class GetProductQueryHandler
{
    private readonly IReadModelRepository _readModelRepo;
    private readonly IMapper _mapper;

    public GetProductQueryHandler(
        IReadModelRepository readModelRepo,
        IMapper mapper)
    {
        _readModelRepo = readModelRepo;
        _mapper = mapper;
    }

    public async Task<ProductDto?> HandleAsync(GetProductQuery query)
    {
        // Read from optimized read model
        var readModel = await _readModelRepo.GetByIdAsync(query.ProductId);
        
        if (readModel == null)
            return null;

        return _mapper.Map<ProductDto>(readModel);
    }
}
