using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Infrastructure.Accounts;

namespace TaskManager.Tasks.Api;

internal static class TaskAuthentication
{
    public static void AddTaskAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>(ConfigureBearer);
        services.AddAuthorization();
    }

    private static void ConfigureBearer(JwtBearerOptions options, IConfiguration configuration)
    {
        RsaSecurityKey? key = null;
        var publicKeyPath = configuration["Auth:PublicKeyPath"];
        if (publicKeyPath is not null)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
            key = new RsaSecurityKey(rsa.ExportParameters(false));
        }

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = key,
            ValidIssuer = TaskAccessTokenSettings.Issuer,
            ValidAudience = TaskAccessTokenSettings.Audience,
            ValidAlgorithms = [TaskAccessTokenSettings.Algorithm],
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var userId)
                    || userId == Guid.Empty)
                {
                    context.Fail("Missing or invalid subject.");
                }

                return Task.CompletedTask;
            }
        };
    }
}
