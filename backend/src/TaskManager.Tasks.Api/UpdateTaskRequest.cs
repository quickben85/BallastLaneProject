namespace TaskManager.Tasks.Api;

internal sealed record UpdateTaskRequest(
    string? Title, string? Description, DateOnly? DueDate, string? TimeZone, string? Status);
