namespace TaskManager.IntegrationTests;

internal sealed record TaskResponse(Guid Id, string Title, string? Description, string Status, DateOnly? DueDate);
