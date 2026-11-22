namespace TaskManager.Application.Accounts;

public interface IAccountCredentials
{
    Task<Guid?> VerifyAsync(string email, string password, CancellationToken cancellationToken);
}
