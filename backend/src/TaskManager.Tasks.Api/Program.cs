using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using TaskManager.Infrastructure;

namespace TaskManager.Tasks.Api;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(builder.Configuration.GetSection("Frontend:Origins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location")));
        builder.Services.AddTaskAuthentication();

        var app = builder.Build();
        var connectionString = app.Configuration.GetConnectionString("Tasks");
        if (!string.IsNullOrWhiteSpace(connectionString)
            && string.IsNullOrWhiteSpace(app.Configuration["Auth:PublicKeyPath"]))
        {
            throw new InvalidOperationException("Configure Auth:PublicKeyPath.");
        }

        _ = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        if (app.Environment.IsDevelopment()
            && !string.IsNullOrWhiteSpace(connectionString))
        {
            PostgresMigrations.Apply(connectionString, "Tasks");
        }

        app.UseExceptionHandler();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/_internal/live");
        app.Run();
    }
}
