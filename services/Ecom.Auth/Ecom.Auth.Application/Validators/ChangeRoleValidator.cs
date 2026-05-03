using Ecom.Auth.Application.DTOs;
using FluentValidation;

namespace Ecom.Auth.Application.Validators;

public class ChangeRoleValidator : AbstractValidator<ChangeRoleDto>
{
    public ChangeRoleValidator()
    {
        RuleFor(x => x.RoleId)
            .GreaterThan(0).WithMessage("RoleId must be greater than 0");
    }
}
