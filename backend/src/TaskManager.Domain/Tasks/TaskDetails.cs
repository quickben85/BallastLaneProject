namespace TaskManager.Domain.Tasks;

internal sealed record TaskDetails(string? Title, DateOnly? DueDate, DateOnly Today, DateOnly? ExistingDueDate);
