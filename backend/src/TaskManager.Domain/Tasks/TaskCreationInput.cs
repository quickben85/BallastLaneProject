namespace TaskManager.Domain.Tasks;

internal sealed record TaskCreationInput(string? Title, DateOnly? DueDate, DateOnly Today);
