namespace TaskManager.Domain.Tasks;

public sealed class TaskItem
{
    private TaskItem(Guid id, Guid ownerId, string title, string? description, TaskStatus status, DateOnly? dueDate)
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
    }

    public Guid Id { get; }
    public Guid OwnerId { get; }
    public string Title { get; }
    public string? Description { get; }
    public TaskStatus Status { get; }
    public DateOnly? DueDate { get; }

    public static TaskCreationResult Create(
        Guid ownerId, string? title, string? description, DateOnly? dueDate, DateOnly today)
    {
        var trimmedTitle = title?.Trim();
        var (titleError, dueDateError) = TaskCreationValidator.Validate(new(trimmedTitle, dueDate, today));
        if (titleError is not null || dueDateError is not null)
        {
            return new TaskCreationResult(null, titleError, dueDateError);
        }

        var task = new TaskItem(Guid.NewGuid(), ownerId, trimmedTitle!,
            string.IsNullOrWhiteSpace(description) ? null : description, TaskStatus.Open, dueDate);
        return new TaskCreationResult(task);
    }

    public static TaskItem Restore(
        Guid id, Guid ownerId, string title, string? description, TaskStatus status, DateOnly? dueDate) =>
        new(id, ownerId, title, description, status, dueDate);
}
