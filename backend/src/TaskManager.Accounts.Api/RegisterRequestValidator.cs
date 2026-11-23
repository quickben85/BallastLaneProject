using System.Text.RegularExpressions;
using FluentValidation;

namespace TaskManager.Accounts.Api;

internal sealed partial class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private const string LocalPartAtom = @"[a-zA-Z0-9!#$%&'*+/=?^_\x60{|}~-]+";
    private const string DomainLabel = "[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?";

    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter your email address.")
            .MaximumLength(254).WithMessage("Enter an email address of up to 254 characters.")
            .Must(BePlainEmailAddress).WithMessage("Enter a valid email address.")
            .OverridePropertyName("email");

        RuleFor(request => request.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter your password.")
            .MaximumLength(1024).WithMessage("Enter a password of up to 1,024 characters.")
            .OverridePropertyName("password");
    }

    private static bool BePlainEmailAddress(string? email) =>
        email is not null
        && email.IndexOf('@') is > 0 and <= 64
        && PlainEmailAddress().IsMatch(email);

    // Keep registration compatible with the email syntax used by the Angular sign-in form.
    [GeneratedRegex(@"\A" + LocalPartAtom + @"(?:\." + LocalPartAtom + ")*@" + DomainLabel
        + @"(?:\." + DomainLabel + @")*\z")]
    private static partial Regex PlainEmailAddress();
}
