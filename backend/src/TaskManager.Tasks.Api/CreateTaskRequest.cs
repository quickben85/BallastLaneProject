namespace TaskManager.Tasks.Api;

internal sealed record CreateTaskRequest(string? Title, string? Description, DateOnly? DueDate, string? TimeZone);
