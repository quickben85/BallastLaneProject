using TaskManager.Domain.Tasks;

namespace TaskManager.Tasks.Api;

internal sealed record TaskResource(Guid Id, string Title, string? Description, string Status, DateOnly? DueDate)
{
    public static TaskResource From(TaskItem item) =>
        new(item.Id, item.Title, item.Description, item.Status.ToString().ToLowerInvariant(), item.DueDate);
}
