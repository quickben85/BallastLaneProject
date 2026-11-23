using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using TaskManager.Application.Tasks;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Tasks;
using TaskManager.Web;

namespace TaskManager.Tasks.Api;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddProblemDetails();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        builder.Services.AddExceptionHandler<RequestBindingExceptionHandler>();
        builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
        builder.Services.AddScoped<ICreateTask, CreateTask>();
        builder.Services.AddScoped<ITaskQueries, TaskQueries>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTaskPersistence();
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
        app.MapTaskEndpoints();
        app.MapHealthChecks("/_internal/live");
        app.Run();
    }
}
