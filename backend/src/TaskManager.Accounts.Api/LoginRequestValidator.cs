using FluentValidation;

namespace TaskManager.Accounts.Api;

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter your email address.")
            .MaximumLength(254)
            .EmailAddress().WithMessage("Enter a valid email address.")
            .OverridePropertyName("email");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Enter your password.")
            .MaximumLength(1024)
            .OverridePropertyName("password");
    }
}
