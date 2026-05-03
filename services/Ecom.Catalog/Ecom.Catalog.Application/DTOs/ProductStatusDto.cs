using Ecom.Catalog.Domain.Enums;

namespace Ecom.Catalog.Application.DTOs;

public class ProductStatusDto
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public List<string> ValidNextStates { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class TransitionProductStatusDto
{
    public ProductLifecycleStatus TargetStatus { get; set; }
    public string? Reason { get; set; }
}
