using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Interfaces;

public interface IBrandRepository
{
    Task<IEnumerable<Brand>> GetAllAsync();
}
