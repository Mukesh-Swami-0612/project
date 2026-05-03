namespace Ecom.Workflow.Domain.Rules;

public static class PriceValidationRule
{
    public static bool IsSatisfiedBy(decimal salePrice, decimal mrp) => salePrice <= mrp;
}
