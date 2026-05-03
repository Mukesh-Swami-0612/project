namespace Ecom.Auth.Application.DTOs;

public class AuditLogFilterDto
{
    public string? Email { get; set; }
    public string? Action { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "CreatedAt"; // default
    public string SortOrder { get; set; } = "desc";   // asc / desc
}
