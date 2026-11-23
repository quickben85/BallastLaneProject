namespace TaskManager.Tasks.Api;

internal sealed class CreateTaskRequestValidator()
    : TaskDetailsRequestValidator<CreateTaskRequest>(request => request.Title, request => request.Description,
        request => request.TimeZone);
