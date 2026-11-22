using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.Accounts;

namespace TaskManager.Infrastructure.Accounts;

internal sealed class RsaJwtAccessTokenIssuer : IAccessTokenIssuer, IDisposable
{
    private const int LifetimeSeconds = 900;

    private readonly RSA key;
    private readonly SigningCredentials signingCredentials;

    public RsaJwtAccessTokenIssuer(string privateKeyPath)
    {
        key = RSA.Create();
        try
        {
            key.ImportFromPem(File.ReadAllText(privateKeyPath));
            _ = key.ExportParameters(true);
            signingCredentials = new SigningCredentials(new RsaSecurityKey(key), TaskAccessTokenSettings.Algorithm);
        }
        catch
        {
            key.Dispose();
            throw;
        }
    }

    public IssuedAccessToken Issue(Guid userId)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = TaskAccessTokenSettings.Issuer,
            Audience = TaskAccessTokenSettings.Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddSeconds(LifetimeSeconds),
            SigningCredentials = signingCredentials
        };

        return new IssuedAccessToken(new JsonWebTokenHandler().CreateToken(descriptor), LifetimeSeconds);
    }

    public void Dispose() => key.Dispose();
}
