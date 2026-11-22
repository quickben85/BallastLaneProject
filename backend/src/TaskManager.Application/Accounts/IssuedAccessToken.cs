namespace TaskManager.Application.Accounts;

public sealed record IssuedAccessToken(string Value, int ExpiresInSeconds);
