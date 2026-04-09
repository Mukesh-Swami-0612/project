using Ecom.Auth.Application.DTOs;
using FluentValidation;

namespace Ecom.Auth.Application.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Refresh token is required")
            .MinimumLength(10).WithMessage("Invalid token");
    }
}
