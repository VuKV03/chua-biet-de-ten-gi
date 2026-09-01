using Admin.Application.Auth.Commands;
using FluentValidation;

namespace Admin.Application.Auth.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.accessToken).NotEmpty().WithMessage("Access token không được để trống.");
        RuleFor(x => x.refreshToken).NotEmpty().WithMessage("Refresh token không được để trống.");
    }
}
