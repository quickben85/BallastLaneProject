namespace TaskManager.Application.Accounts;

public sealed record RegisterAccountResult(Guid? Id, AccountRegistrationFailure? Failure = null);
