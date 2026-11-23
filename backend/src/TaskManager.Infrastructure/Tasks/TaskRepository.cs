using System.Data;
using Dapper;
using Npgsql;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Tasks;

namespace TaskManager.Infrastructure.Tasks;

internal sealed class TaskRepository(NpgsqlDataSource dataSource) : ITaskRepository
{
    private const string TaskColumns = """
        SELECT id, owner_id AS OwnerId, title, description, status, due_date AS DueDate
        FROM tasks.tasks
        """;

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tasks.tasks (id, owner_id, title, description, status, due_date)
            VALUES (@Id, @OwnerId, @Title, @Description, @Status, @DueDate)
            """;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, WriteParameters(task), cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(TaskItem task, bool requireCurrentDueDate, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tasks.tasks
            SET title = @Title, description = @Description, status = @Status, due_date = @DueDate
            WHERE id = @Id AND owner_id = @OwnerId
                AND (NOT @RequireCurrentDueDate OR due_date = @DueDate)
            """;
        var parameters = WriteParameters(task);
        parameters.Add("RequireCurrentDueDate", requireCurrentDueDate);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(
            sql, parameters, cancellationToken: cancellationToken)) == 1;
    }

    public async Task<IReadOnlyList<TaskItem>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var sql = TaskColumns + " WHERE owner_id = @ownerId ORDER BY title, id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<TaskRow>(new CommandDefinition(
            sql, new { ownerId }, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToDomain()).ToArray();
    }

    public async Task<TaskItem?> FindForOwnerAsync(Guid taskId, Guid ownerId, CancellationToken cancellationToken)
    {
        var sql = TaskColumns + " WHERE id = @taskId AND owner_id = @ownerId";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<TaskRow>(new CommandDefinition(
            sql, new { taskId, ownerId }, cancellationToken: cancellationToken));
        return row?.ToDomain();
    }

    private static DynamicParameters WriteParameters(TaskItem task)
    {
        var parameters = new DynamicParameters(new
        {
            task.Id,
            task.OwnerId,
            task.Title,
            task.Description,
            Status = task.Status.ToString().ToLowerInvariant()
        });
        parameters.Add("DueDate", task.DueDate?.ToDateTime(TimeOnly.MinValue), DbType.Date);
        return parameters;
    }
}
