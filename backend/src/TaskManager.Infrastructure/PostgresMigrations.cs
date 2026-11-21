using DbUp;

namespace TaskManager.Infrastructure;

public static class PostgresMigrations
{
    public static void Apply(string connectionString, string migrationSet)
    {
        var prefix = $"TaskManager.Infrastructure.Migrations.{migrationSet}.";
        var upgrade = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(PostgresMigrations).Assembly,
                name => name.StartsWith(prefix, StringComparison.Ordinal))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrade.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException($"Failed to migrate the {migrationSet} database.", result.Error);
        }
    }
}
