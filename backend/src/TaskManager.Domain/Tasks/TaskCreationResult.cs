namespace TaskManager.Domain.Tasks;

public sealed record TaskCreationResult(TaskItem? Item, string? TitleError = null, string? DueDateError = null);
