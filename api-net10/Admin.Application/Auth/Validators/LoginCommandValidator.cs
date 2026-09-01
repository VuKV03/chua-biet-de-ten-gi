using Admin.Application.Auth.Commands;
using FluentValidation;

namespace Admin.Application.Auth.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.taiKhoan)
            .NotEmpty().WithMessage("Tài khoản không được để trống.")
            .MaximumLength(255);

        RuleFor(x => x.matKhau)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}
