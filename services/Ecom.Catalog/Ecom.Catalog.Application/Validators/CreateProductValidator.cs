using Ecom.Catalog.Application.DTOs;
using FluentValidation;

namespace Ecom.Catalog.Application.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.CreatedBy).GreaterThan(0);
    }
}
