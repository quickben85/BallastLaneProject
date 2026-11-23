using TaskManager.Domain.Tasks;
using TaskStatus = TaskManager.Domain.Tasks.TaskStatus;

namespace TaskManager.Application.Tasks;

public sealed class UpdateTask(ITaskRepository tasks, TimeProvider clock) : IUpdateTask
{
    public async Task<TaskWriteResult?> ExecuteAsync(Guid id, Guid ownerId, string? title, string? description,
        TaskStatus status, DateOnly? dueDate, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var current = await tasks.FindForOwnerAsync(id, ownerId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone).DateTime);
        var result = current.Update(title, description, status, dueDate, today);
        if (result.Item is not { } updated)
        {
            return result;
        }

        // Only an already stored date may be kept when it is in the past.
        var requireCurrentDueDate = dueDate < today;
        if (await tasks.UpdateAsync(updated, requireCurrentDueDate, cancellationToken))
        {
            return result;
        }

        if (!requireCurrentDueDate || await tasks.FindForOwnerAsync(id, ownerId, cancellationToken) is null)
        {
            return null;
        }

        return new TaskWriteResult(null, DueDateError: "The stored due date changed. Enter a new date or clear it.");
    }
}
