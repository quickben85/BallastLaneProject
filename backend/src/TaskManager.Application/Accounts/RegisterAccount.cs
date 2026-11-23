namespace TaskManager.Application.Accounts;

public sealed class RegisterAccount(IAccountRegistration accounts) : IRegisterAccount
{
    public async Task<RegisterAccountResult> ExecuteAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var failure = await accounts.TryCreateAsync(id, email, password, cancellationToken);
        return failure is null ? new(id) : new(null, failure);
    }
}
