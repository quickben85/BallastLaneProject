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

    public static TaskWriteResult Create(
        Guid ownerId, string? title, string? description, DateOnly? dueDate, DateOnly today) =>
        Write(Guid.NewGuid(), ownerId, title, description, TaskStatus.Open, dueDate, today, existingDueDate: null);

    public TaskWriteResult Update(
        string? title, string? description, TaskStatus status, DateOnly? dueDate, DateOnly today) =>
        Write(Id, OwnerId, title, description, status, dueDate, today, DueDate);

    private static TaskWriteResult Write(Guid id, Guid ownerId, string? title, string? description,
        TaskStatus status, DateOnly? dueDate, DateOnly today, DateOnly? existingDueDate)
    {
        var trimmedTitle = title?.Trim();
        var (titleError, dueDateError) = TaskDetailsValidator.Validate(new(trimmedTitle, dueDate, today, existingDueDate));
        if (titleError is not null || dueDateError is not null)
        {
            return new TaskWriteResult(null, titleError, dueDateError);
        }

        var task = new TaskItem(id, ownerId, trimmedTitle!,
            string.IsNullOrWhiteSpace(description) ? null : description, status, dueDate);
        return new TaskWriteResult(task);
    }

    public static TaskItem Restore(
        Guid id, Guid ownerId, string title, string? description, TaskStatus status, DateOnly? dueDate) =>
        new(id, ownerId, title, description, status, dueDate);
}
