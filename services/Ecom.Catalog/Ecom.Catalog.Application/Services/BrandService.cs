using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;

namespace Ecom.Catalog.Application.Services;

public class BrandService
{
    private readonly IBrandRepository _brandRepo;
    private readonly IMapper _mapper;

    public BrandService(IBrandRepository brandRepo, IMapper mapper)
    {
        _brandRepo = brandRepo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BrandDto>> GetAllAsync()
    {
        var brands = await _brandRepo.GetAllAsync();
        return _mapper.Map<IEnumerable<BrandDto>>(brands);
    }
}
