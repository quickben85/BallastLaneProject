namespace TaskManager.Application.Accounts;

public interface IRegisterAccount
{
    Task<RegisterAccountResult> ExecuteAsync(string email, string password, CancellationToken cancellationToken);
}
