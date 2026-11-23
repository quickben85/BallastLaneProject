namespace TaskManager.Domain.Tasks;

public sealed record TaskWriteResult(TaskItem? Item, string? TitleError = null, string? DueDateError = null);
