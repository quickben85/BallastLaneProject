namespace TaskManager.Application.Accounts;

public sealed record AccountRegistrationFailure(
    bool DuplicateEmail = false, string? EmailError = null, IReadOnlyList<string>? PasswordErrors = null);
