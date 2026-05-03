using Ecom.Auth.Application.DTOs;
using FluentValidation;

namespace Ecom.Auth.Application.Validators;

public class SignupRequestValidator : AbstractValidator<SignupRequestDto>
{
    public SignupRequestValidator()
    {
        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100);

        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        // Confirm Password
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x)
            .Must(request => request.AdditionalData == null ||
                             !request.AdditionalData.Keys.Any(key => string.Equals(key, "roleId", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Role assignment is not allowed during signup.");
    }
}
