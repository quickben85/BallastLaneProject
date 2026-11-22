namespace TaskManager.Application.Accounts;

public sealed class SignIn(IAccountCredentials credentials, IAccessTokenIssuer tokens) : ISignIn
{
    public async Task<IssuedAccessToken?> ExecuteAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var userId = await credentials.VerifyAsync(email, password, cancellationToken);
        return userId is Guid id ? tokens.Issue(id) : null;
    }
}
