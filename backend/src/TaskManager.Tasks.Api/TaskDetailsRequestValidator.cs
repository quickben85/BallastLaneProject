using System.Linq.Expressions;
using FluentValidation;

namespace TaskManager.Tasks.Api;

internal abstract class TaskDetailsRequestValidator<TRequest> : AbstractValidator<TRequest>
{
    protected TaskDetailsRequestValidator(Expression<Func<TRequest, string?>> title,
        Expression<Func<TRequest, string?>> description, Expression<Func<TRequest, string?>> timeZone)
    {
        RuleFor(title)
            .Must(HaveNoNullCharacter).WithMessage("Remove the null character from the title.")
            .OverridePropertyName("title");

        RuleFor(description)
            .Must(HaveNoNullCharacter).WithMessage("Remove the null character from the description.")
            .OverridePropertyName("description");

        RuleFor(timeZone)
            .Must(zone => zone is null || TimeZoneInfo.TryFindSystemTimeZoneById(zone, out _))
            .WithMessage("Enter a recognized time zone.")
            .OverridePropertyName("timeZone");
    }

    private static bool HaveNoNullCharacter(string? value) => value?.Contains('\0') != true;
}
