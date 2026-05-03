using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;

namespace Ecom.Catalog.Application.Services;

public class StorefrontService
{
    private readonly IReadModelRepository _readModelRepository;
    private readonly IMapper _mapper;

    public StorefrontService(IReadModelRepository readModelRepository, IMapper mapper)
    {
        _readModelRepository = readModelRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductReadModelDto>> GetPublishedProductsAsync()
    {
        var models = await _readModelRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductReadModelDto>>(models);
    }

    public async Task<ProductReadModelDto?> GetProductPreviewAsync(int id)
    {
        var model = await _readModelRepository.GetByIdAsync(id);
        return model == null ? null : _mapper.Map<ProductReadModelDto>(model);
    }
}
