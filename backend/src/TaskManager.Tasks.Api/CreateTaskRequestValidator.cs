using FluentValidation;

namespace TaskManager.Tasks.Api;

internal sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .Must(HaveNoNullCharacter).WithMessage("Remove the null character from the title.")
            .OverridePropertyName("title");

        RuleFor(request => request.Description)
            .Must(HaveNoNullCharacter).WithMessage("Remove the null character from the description.")
            .OverridePropertyName("description");

        RuleFor(request => request.TimeZone)
            .Must(timeZone => timeZone is null || TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            .WithMessage("Enter a recognized time zone.")
            .OverridePropertyName("timeZone");
    }

    private static bool HaveNoNullCharacter(string? value) => value?.Contains('\0') != true;
}
