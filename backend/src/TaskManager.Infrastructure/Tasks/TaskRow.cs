using TaskManager.Domain.Tasks;
using TaskStatus = TaskManager.Domain.Tasks.TaskStatus;

namespace TaskManager.Infrastructure.Tasks;

internal sealed record TaskRow(Guid Id, Guid OwnerId, string Title, string? Description, string Status, DateOnly? DueDate)
{
    public TaskItem ToDomain() =>
        TaskItem.Restore(Id, OwnerId, Title, Description, Enum.Parse<TaskStatus>(Status, ignoreCase: true), DueDate);
}
