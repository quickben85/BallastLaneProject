using FluentValidation;
using TaskManager.Application.Accounts;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Accounts;

namespace TaskManager.Accounts.Api;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddProblemDetails();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        builder.Services.AddExceptionHandler<RequestBindingExceptionHandler>();
        builder.Services.AddHealthChecks();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(builder.Configuration.GetSection("Frontend:Origins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()));
        builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        builder.Services.AddScoped<ISignIn, SignIn>();
        builder.Services.AddAccountServices();

        var app = builder.Build();
        var connectionString = app.Configuration.GetConnectionString("Accounts");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _ = app.Services.GetRequiredService<IAccessTokenIssuer>();
            if (app.Environment.IsDevelopment())
            {
                PostgresMigrations.Apply(connectionString, "Accounts");
                if (app.Configuration.GetValue<bool>("Demo:Seed"))
                {
                    await app.Services.SeedDemoAccountAsync();
                }
            }
        }

        app.UseExceptionHandler();
        app.UseCors();
        app.MapPost("/api/accounts/login", LoginEndpoint.HandleAsync)
            .AllowAnonymous();
        app.MapHealthChecks("/_internal/live");
        await app.RunAsync();
    }
}
