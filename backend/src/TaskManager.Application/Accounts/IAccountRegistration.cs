namespace TaskManager.Application.Accounts;

public interface IAccountRegistration
{
    Task<AccountRegistrationFailure?> TryCreateAsync(
        Guid id, string email, string password, CancellationToken cancellationToken);
}
