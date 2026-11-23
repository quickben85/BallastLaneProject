using Microsoft.AspNetCore.Identity;
using TaskManager.Application.Accounts;

namespace TaskManager.Infrastructure.Accounts;

internal sealed class IdentityAccountRegistration(UserManager<IdentityUser<Guid>> users) : IAccountRegistration
{
    public async Task<AccountRegistrationFailure?> TryCreateAsync(
        Guid id, string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = new IdentityUser<Guid> { Id = id, UserName = email };
        var result = await users.CreateAsync(user, password);
        if (result.Succeeded)
        {
            return null;
        }

        if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return new(DuplicateEmail: true);
        }

        var passwordErrors = result.Errors
            .Where(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
            .Select(error => error.Description)
            .ToArray();
        var emailError = result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.InvalidUserName))
            ? "Enter a valid email address."
            : null;
        if (emailError is null && passwordErrors.Length == 0)
        {
            throw new InvalidOperationException("Identity returned an unexpected registration result.");
        }

        return new(EmailError: emailError, PasswordErrors: passwordErrors);
    }
}
