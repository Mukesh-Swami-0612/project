namespace Ecom.Catalog.Domain.Entities;

public class MediaAsset
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public bool IsPrimary { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public string? AltText { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public Product Product { get; set; } = null!;
}
