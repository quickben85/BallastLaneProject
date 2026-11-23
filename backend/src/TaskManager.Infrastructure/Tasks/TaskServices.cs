using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskManager.Application.Tasks;

namespace TaskManager.Infrastructure.Tasks;

public static class TaskServices
{
    public static void AddTaskPersistence(this IServiceCollection services)
    {
        services.AddSingleton(provider => NpgsqlDataSource.Create(
            provider.GetRequiredService<IConfiguration>().GetConnectionString("Tasks")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Tasks.")));
        services.AddScoped<ITaskRepository, TaskRepository>();
    }
}
