namespace Ecom.Catalog.Domain.Entities;

public class MigrationLog
{
    public int Id { get; set; }
    public string MigrationName { get; set; } = string.Empty;
    public string AppliedBy { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public string? Details { get; set; }
}
