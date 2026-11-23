using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using AccountsHost = TaskManager.Accounts.Api.Program;
using TasksHost = TaskManager.Tasks.Api.Program;

namespace TaskManager.IntegrationTests;

public sealed class LoginFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("accounts")
        .Build();
    private readonly DirectoryInfo keys = Directory.CreateTempSubdirectory("task-manager-sign-in-");

    public WebApplicationFactory<AccountsHost> Accounts { get; private set; } = null!;
    public WebApplicationFactory<TasksHost> Tasks { get; private set; } = null!;
    public string TasksConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        await using (var connection = new NpgsqlConnection(database.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("CREATE DATABASE tasks", connection);
            await command.ExecuteNonQueryAsync();
        }

        TasksConnectionString = new NpgsqlConnectionStringBuilder(database.GetConnectionString())
        {
            Database = "tasks"
        }.ConnectionString;

        using var rsa = RSA.Create(2048);
        var privatePath = Path.Combine(keys.FullName, "private.pem");
        var publicPath = Path.Combine(keys.FullName, "public.pem");
        await File.WriteAllTextAsync(privatePath, rsa.ExportPkcs8PrivateKeyPem());
        await File.WriteAllTextAsync(publicPath, rsa.ExportSubjectPublicKeyInfoPem());

        Accounts = new ConfiguredHost<AccountsHost>(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Accounts"] = database.GetConnectionString(),
            ["Auth:PrivateKeyPath"] = privatePath,
            ["Demo:Seed"] = "true",
            ["Frontend:Origins:0"] = "http://localhost:8088",
            ["Frontend:Origins:1"] = "http://127.0.0.1:8088",
            ["Frontend:Origins:2"] = "http://localhost:4200",
            ["Frontend:Origins:3"] = "http://127.0.0.1:4200"
        });
        Tasks = new ConfiguredHost<TasksHost>(new Dictionary<string, string?>
        {
            ["Auth:PublicKeyPath"] = publicPath,
            ["ConnectionStrings:Tasks"] = TasksConnectionString,
            ["Frontend:Origins:0"] = "http://localhost:8088",
            ["Frontend:Origins:1"] = "http://127.0.0.1:8088",
            ["Frontend:Origins:2"] = "http://localhost:4200",
            ["Frontend:Origins:3"] = "http://127.0.0.1:4200"
        });
    }

    public async Task DisposeAsync()
    {
        if (Tasks is not null) await Tasks.DisposeAsync();
        if (Accounts is not null) await Accounts.DisposeAsync();
        await database.DisposeAsync();
        keys.Delete(recursive: true);
    }
}
