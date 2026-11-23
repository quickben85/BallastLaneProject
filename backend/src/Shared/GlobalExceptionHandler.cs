using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaskManager.Web;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler, IDeveloperPageExceptionFilter
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        await WriteProblemAsync(context, exception, cancellationToken);
        return true;
    }

    public Task HandleExceptionAsync(ErrorContext error, Func<ErrorContext, Task> _) =>
        WriteProblemAsync(error.HttpContext, error.Exception, error.HttpContext.RequestAborted);

    private Task WriteProblemAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        logger.LogError(exception, "Unhandled API exception for {Method}. Trace ID {TraceId}",
            context.Request.Method, traceId);

        context.Response.Headers.CacheControl = "no-store";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Extensions = { ["traceId"] = traceId }
        };
        return context.Response.WriteAsJsonAsync(problem, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
    }
}
