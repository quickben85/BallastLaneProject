using Microsoft.IdentityModel.Tokens;

namespace TaskManager.Infrastructure.Accounts;

public static class TaskAccessTokenSettings
{
    public const string Issuer = "TaskManager.Accounts";
    public const string Audience = "TaskManager.Tasks";
    public const string Algorithm = SecurityAlgorithms.RsaSha256;
}
