namespace Ecom.Catalog.Domain.Entities;

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
