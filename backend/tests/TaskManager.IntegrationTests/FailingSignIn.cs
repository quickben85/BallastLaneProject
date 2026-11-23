using TaskManager.Application.Accounts;

namespace TaskManager.IntegrationTests;

internal sealed class FailingSignIn : ISignIn
{
    public Task<IssuedAccessToken?> ExecuteAsync(string email, string password, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Internal sign-in exception marker.");
}
