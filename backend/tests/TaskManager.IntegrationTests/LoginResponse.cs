namespace TaskManager.IntegrationTests;

internal sealed record LoginResponse(string AccessToken, int ExpiresInSeconds);
