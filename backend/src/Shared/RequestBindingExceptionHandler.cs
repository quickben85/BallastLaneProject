using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace TaskManager.Web;

internal sealed class RequestBindingExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException
            { StatusCode: >= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError } badRequest)
        {
            return false;
        }

        context.Response.Headers.CacheControl = "no-store";
        if (badRequest.StatusCode != StatusCodes.Status400BadRequest)
        {
            await Results.Problem(statusCode: badRequest.StatusCode).ExecuteAsync(context);
            return true;
        }

        var field = GetErrorField((badRequest.InnerException as JsonException)?.Path);
        var message = field == "body"
            ? "Send a valid JSON request body."
            : "Enter a valid value for this field.";

        await Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] })
            .ExecuteAsync(context);
        return true;
    }

    private static string GetErrorField(string? path)
    {
        if (path is null || path.Length <= 2 || !path.StartsWith("$.", StringComparison.Ordinal))
        {
            return "body";
        }

        var field = path[2..];
        return field.All(character => char.IsAsciiLetterOrDigit(character) || character == '_')
            ? JsonNamingPolicy.CamelCase.ConvertName(field)
            : "body";
    }
}
