using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Tasks;
using TaskStatus = TaskManager.Domain.Tasks.TaskStatus;

namespace TaskManager.Tasks.Api;

internal static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        var tasks = app.MapGroup("/api/tasks").RequireAuthorization();
        tasks.MapPost("", CreateAsync);
        tasks.MapGet("", ListAsync);
        tasks.MapGet("/{id:guid}", GetAsync).WithName("GetTaskById");
        tasks.MapPut("/{id:guid}", UpdateAsync);
        tasks.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateTaskRequest request, IValidator<CreateTaskRequest> validator,
        ICreateTask createTask, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var zone = request.TimeZone is null ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
        var result = await createTask.ExecuteAsync(OwnerId(user), request.Title, request.Description,
            request.DueDate, zone, cancellationToken);
        if (result.Item is { } task)
        {
            return Results.CreatedAtRoute("GetTaskById", new { id = task.Id }, TaskResource.From(task));
        }

        return WriteValidationProblem(result);
    }

    private static async Task<IResult> UpdateAsync(Guid id, [FromBody] UpdateTaskRequest request,
        IValidator<UpdateTaskRequest> validator, IUpdateTask updateTask, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var zone = request.TimeZone is null ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
        var status = request.Status == "open" ? TaskStatus.Open : TaskStatus.Completed;
        var result = await updateTask.ExecuteAsync(id, OwnerId(user), request.Title, request.Description, status,
            request.DueDate, zone, cancellationToken);
        if (result is null)
        {
            return Results.NotFound();
        }

        return result.Item is { } task ? Results.Ok(TaskResource.From(task)) : WriteValidationProblem(result);
    }

    private static IResult WriteValidationProblem(TaskWriteResult result)
    {
        var errors = new Dictionary<string, string[]>();
        if (result.TitleError is { } titleError)
        {
            errors.Add("title", [titleError]);
        }
        if (result.DueDateError is { } dueDateError)
        {
            errors.Add("dueDate", [dueDateError]);
        }
        return Results.ValidationProblem(errors);
    }

    private static async Task<IResult> ListAsync(
        ITaskQueries queries, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var tasks = await queries.ListAsync(OwnerId(user), cancellationToken);
        return Results.Ok(tasks.Select(TaskResource.From));
    }

    private static async Task<IResult> GetAsync(
        Guid id, ITaskQueries queries, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var task = await queries.GetAsync(id, OwnerId(user), cancellationToken);
        return task is null ? Results.NotFound() : Results.Ok(TaskResource.From(task));
    }

    private static Guid OwnerId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);

    private static async Task<IResult> DeleteAsync(
        Guid id, IDeleteTask deleteTask, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var deleted = await deleteTask.ExecuteAsync(id, OwnerId(user), cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
