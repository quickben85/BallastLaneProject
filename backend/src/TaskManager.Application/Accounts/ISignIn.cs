namespace TaskManager.Application.Accounts;

public interface ISignIn
{
    Task<IssuedAccessToken?> ExecuteAsync(string email, string password, CancellationToken cancellationToken);
}
