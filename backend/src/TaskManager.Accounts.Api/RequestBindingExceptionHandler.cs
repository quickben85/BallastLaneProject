using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace TaskManager.Accounts.Api;

internal sealed class RequestBindingExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException { StatusCode: StatusCodes.Status400BadRequest } badRequest)
        {
            return false;
        }

        var field = GetErrorField((badRequest.InnerException as JsonException)?.Path);
        var message = field == "body"
            ? "Send a valid JSON request body."
            : "Enter a valid value for this field.";

        context.Response.Headers.CacheControl = "no-store";
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
