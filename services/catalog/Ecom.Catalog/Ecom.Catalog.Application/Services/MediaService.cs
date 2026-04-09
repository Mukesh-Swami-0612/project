using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Services;

public class MediaService
{
    private static readonly string[] AllowedTypes = { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly IMediaRepository _repo;
    private readonly IMapper _mapper;

    public MediaService(IMediaRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MediaAssetDto>> GetByProductIdAsync(int productId)
    {
        var media = await _repo.GetByProductIdAsync(productId);
        return _mapper.Map<IEnumerable<MediaAssetDto>>(media);
    }

    public async Task AddMediaAsync(int productId, string fileUrl, string fileType, bool isPrimary, string? altText)
    {
        var ext = Path.GetExtension(fileUrl).ToLowerInvariant();
        if (!AllowedTypes.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var asset = new MediaAsset
        {
            ProductId = productId,
            FileUrl = fileUrl,
            FileType = fileType,
            IsPrimary = isPrimary,
            AltText = altText
        };
        await _repo.AddAsync(asset);
    }
}
