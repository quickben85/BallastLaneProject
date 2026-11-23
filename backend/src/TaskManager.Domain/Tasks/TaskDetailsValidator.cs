using FluentValidation;

namespace TaskManager.Domain.Tasks;

internal static class TaskDetailsValidator
{
    private static readonly IValidator<TaskDetails> Validator = CreateValidator();

    public static (string? TitleError, string? DueDateError) Validate(TaskDetails input)
    {
        var result = Validator.Validate(input);
        return (
            result.Errors.FirstOrDefault(error => error.PropertyName == nameof(TaskDetails.Title))?.ErrorMessage,
            result.Errors.FirstOrDefault(error => error.PropertyName == nameof(TaskDetails.DueDate))?.ErrorMessage);
    }

    private static IValidator<TaskDetails> CreateValidator()
    {
        var validator = new InlineValidator<TaskDetails>();
        validator.RuleFor(task => task.Title)
            .Must(title => title is { Length: >= 1 and <= 120 })
            .WithMessage("Enter a title of 1–120 characters.");
        validator.RuleFor(task => task.DueDate)
            .Must((task, date) => date is null || date == task.ExistingDueDate || date >= task.Today)
            .WithMessage("Enter a due date of today or later.");
        return validator;
    }
}
