using TaskManager.Domain.Tasks;
using TaskStatus = TaskManager.Domain.Tasks.TaskStatus;

namespace TaskManager.Application.Tasks;

public interface IUpdateTask
{
    Task<TaskWriteResult?> ExecuteAsync(Guid id, Guid ownerId, string? title, string? description,
        TaskStatus status, DateOnly? dueDate, TimeZoneInfo timeZone, CancellationToken cancellationToken);
}
