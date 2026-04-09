using Ecom.Workflow.Application.DTOs;
using FluentValidation;

namespace Ecom.Workflow.Application.Validators;

public class PublishChecklistValidator : AbstractValidator<PublishChecklistDto>
{
    public PublishChecklistValidator()
    {
        RuleFor(x => x.HasMedia).Equal(true).WithMessage("Product must have at least one media asset.");
        RuleFor(x => x.HasPricing).Equal(true).WithMessage("Product must have pricing configured.");
        RuleFor(x => x.HasInventory).Equal(true).WithMessage("Product must have inventory set.");
        RuleFor(x => x.HasDescription).Equal(true).WithMessage("Product must have a description.");
        RuleFor(x => x.IsApproved).Equal(true).WithMessage("Product must be approved before publishing.");
    }
}
