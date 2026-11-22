using TaskManager.Application.Accounts;

namespace TaskManager.UnitTests;

internal sealed class RecordingTokenIssuer : IAccessTokenIssuer
{
    public Guid? Subject { get; private set; }

    public IssuedAccessToken Issue(Guid userId)
    {
        Subject = userId;
        return new IssuedAccessToken("access-token", 900);
    }
}
