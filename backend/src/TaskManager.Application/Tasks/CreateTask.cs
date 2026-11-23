using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public sealed class CreateTask(ITaskRepository tasks, TimeProvider clock) : ICreateTask
{
    public async Task<TaskWriteResult> ExecuteAsync(Guid ownerId, string? title, string? description,
        DateOnly? dueDate, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone).DateTime);
        var result = TaskItem.Create(ownerId, title, description, dueDate, today);
        if (result.Item is not null)
        {
            await tasks.AddAsync(result.Item, cancellationToken);
        }

        return result;
    }
}
