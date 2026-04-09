using Ecom.Workflow.Application.DTOs;
using FluentValidation;

namespace Ecom.Workflow.Application.Validators;

public class PricingValidator : AbstractValidator<PricingDto>
{
    public PricingValidator()
    {
        RuleFor(x => x.MRP).GreaterThan(0);
        RuleFor(x => x.SalePrice).GreaterThan(0)
            .LessThanOrEqualTo(x => x.MRP).WithMessage("Sale price must not exceed MRP.");
        RuleFor(x => x.ProductVariantId).GreaterThan(0);
    }
}
