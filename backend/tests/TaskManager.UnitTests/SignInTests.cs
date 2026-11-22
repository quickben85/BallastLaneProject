using TaskManager.Application.Accounts;

namespace TaskManager.UnitTests;

public sealed class SignInTests
{
    [Fact]
    public async Task IssuesTokenForVerifiedUser()
    {
        var ownerId = Guid.NewGuid();
        var tokens = new RecordingTokenIssuer();
        var signIn = new SignIn(new StubCredentials(ownerId), tokens);

        var token = await signIn.ExecuteAsync("demo@example.test", "password", CancellationToken.None);

        Assert.Equal(ownerId, tokens.Subject);
        Assert.Equal(new IssuedAccessToken("access-token", 900), token);
    }

    [Fact]
    public async Task FailedVerificationDoesNotIssueToken()
    {
        var tokens = new RecordingTokenIssuer();
        var signIn = new SignIn(new StubCredentials(null), tokens);

        var token = await signIn.ExecuteAsync("demo@example.test", "wrong", CancellationToken.None);

        Assert.Null(token);
        Assert.Null(tokens.Subject);
    }
}
