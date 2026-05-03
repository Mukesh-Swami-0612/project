namespace Ecom.Reporting.Application.DTOs;

public class ReportExportRequestDto
{
    public string ReportType { get; set; } = string.Empty; // e.g. "catalog-quality", "low-stock", "price-changes"
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Format { get; set; } = "csv"; // csv | excel
}
