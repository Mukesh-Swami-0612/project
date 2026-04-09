namespace Ecom.Catalog.Application.DTOs;

public class CreateMediaAssetDto
{
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = "image";
    public bool IsPrimary { get; set; }
    public string? AltText { get; set; }
}
