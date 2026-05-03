using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Interfaces;

public interface IMediaRepository
{
    Task<IEnumerable<MediaAsset>> GetByProductIdAsync(int productId);
    Task AddAsync(MediaAsset asset);
    Task DeleteAsync(int id);
}
