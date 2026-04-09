using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Interfaces;

public interface IReadModelRepository
{
    Task<IEnumerable<ProductReadModel>> GetAllAsync();
    Task<ProductReadModel?> GetByIdAsync(int id);
    Task UpsertAsync(ProductReadModel model);
}
