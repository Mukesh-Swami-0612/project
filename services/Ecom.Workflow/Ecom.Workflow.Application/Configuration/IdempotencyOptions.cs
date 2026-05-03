namespace Ecom.Workflow.Application.Configuration;

/// <summary>
/// Configuration options for idempotency enforcement
/// Phase 2: Feature flag to enable/disable duplicate prevention
/// </summary>
public class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>
    /// When true, duplicate events are skipped (not processed)
    /// When false, duplicates are logged but still processed (Phase 1 behavior)
    /// </summary>
    public bool Enforce { get; set; } = false;
}
