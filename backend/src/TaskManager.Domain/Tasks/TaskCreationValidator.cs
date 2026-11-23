using FluentValidation;

namespace TaskManager.Domain.Tasks;

internal static class TaskCreationValidator
{
    private static readonly IValidator<TaskCreationInput> Validator = CreateValidator();

    public static (string? TitleError, string? DueDateError) Validate(TaskCreationInput input)
    {
        var result = Validator.Validate(input);
        return (
            result.Errors.FirstOrDefault(error => error.PropertyName == nameof(TaskCreationInput.Title))?.ErrorMessage,
            result.Errors.FirstOrDefault(error => error.PropertyName == nameof(TaskCreationInput.DueDate))?.ErrorMessage);
    }

    private static IValidator<TaskCreationInput> CreateValidator()
    {
        var validator = new InlineValidator<TaskCreationInput>();
        validator.RuleFor(task => task.Title)
            .Must(title => title is { Length: >= 1 and <= 120 })
            .WithMessage("Enter a title of 1–120 characters.");
        validator.RuleFor(task => task.DueDate)
            .Must((task, date) => date is null || date >= task.Today)
            .WithMessage("Enter a due date of today or later.");
        return validator;
    }
}
