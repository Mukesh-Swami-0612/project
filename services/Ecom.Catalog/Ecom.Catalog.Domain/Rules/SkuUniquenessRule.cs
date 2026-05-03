namespace Ecom.Catalog.Domain.Rules;

public static class SkuUniquenessRule
{
    public static bool IsSatisfiedBy(string sku, IEnumerable<string> existingSkus) =>
        !existingSkus.Contains(sku, StringComparer.OrdinalIgnoreCase);
}
