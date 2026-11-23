using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public interface ICreateTask
{
    Task<TaskWriteResult> ExecuteAsync(Guid ownerId, string? title, string? description,
        DateOnly? dueDate, TimeZoneInfo timeZone, CancellationToken cancellationToken);
}
