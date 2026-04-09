using Ecom.Auth.Application.DTOs;
using FluentValidation;

namespace Ecom.Auth.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        // Current Password validation
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        // New Password validation
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("New password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("New password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("New password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

        // Confirm New Password
        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}
