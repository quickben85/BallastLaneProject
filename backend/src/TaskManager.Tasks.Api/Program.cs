using TaskManager.Infrastructure;

namespace TaskManager.Tasks.Api;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        if (app.Environment.IsDevelopment()
            && app.Configuration.GetConnectionString("Tasks") is { Length: > 0 } connectionString)
        {
            PostgresMigrations.Apply(connectionString, "Tasks");
        }

        app.UseExceptionHandler();
        app.MapHealthChecks("/_internal/live");
        app.Run();
    }
}
