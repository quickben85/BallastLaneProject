using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskManager.Application.Accounts;

namespace TaskManager.Infrastructure.Accounts;

public static class AccountServices
{
    public static void AddAccountServices(this IServiceCollection services)
    {
        services.AddSingleton(provider => NpgsqlDataSource.Create(
            provider.GetRequiredService<IConfiguration>().GetConnectionString("Accounts")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Accounts.")));

        services.AddIdentityCore<IdentityUser<Guid>>(options =>
            // The Accounts API validates email addresses before using them as usernames.
            options.User.AllowedUserNameCharacters = string.Empty)
            .AddUserStore<DapperUserStore>();
        services.AddScoped<IAccountCredentials, IdentityAccountCredentials>();
        services.AddScoped<IAccountRegistration, IdentityAccountRegistration>();
        services.AddSingleton<IAccessTokenIssuer>(provider => new RsaJwtAccessTokenIssuer(
            provider.GetRequiredService<IConfiguration>()["Auth:PrivateKeyPath"]
            ?? throw new InvalidOperationException("Configure Auth:PrivateKeyPath.")));
    }

    public static async Task SeedDemoAccountAsync(this IServiceProvider services)
    {
        const string email = "demo@example.test";

        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        if (await users.FindByNameAsync(email) is not null)
        {
            return;
        }

        var user = new IdentityUser<Guid>
        {
            Id = Guid.Parse("ea9dd2dc-5959-4ca7-b98d-a38923c31aff"),
            UserName = email
        };

        var result = await users.CreateAsync(user, "DemoTasks!2026");
        if (!result.Succeeded && await users.FindByNameAsync(email) is null)
        {
            throw new InvalidOperationException("Could not seed the demo account: "
                + string.Join(", ", result.Errors.Select(error => error.Code)));
        }
    }
}
