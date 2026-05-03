namespace Ecom.Catalog.Application.DTOs;

public class MediaAssetDto
{
    public int Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public bool IsPrimary { get; set; }
    public string? AltText { get; set; }
}
