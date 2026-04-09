using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;

namespace Ecom.Catalog.Application.Services;

public class CategoryService
{
    private readonly ICategoryRepository _repo;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
}
