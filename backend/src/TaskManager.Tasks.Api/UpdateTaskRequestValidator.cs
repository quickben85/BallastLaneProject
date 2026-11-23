using FluentValidation;

namespace TaskManager.Tasks.Api;

internal sealed class UpdateTaskRequestValidator : TaskDetailsRequestValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator() : base(request => request.Title, request => request.Description,
        request => request.TimeZone)
    {
        RuleFor(request => request.Status)
            .Must(status => status is "open" or "completed")
            .WithMessage("Choose Open or Completed.")
            .OverridePropertyName("status");
    }
}
