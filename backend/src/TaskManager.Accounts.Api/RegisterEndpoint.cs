using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Accounts;

namespace TaskManager.Accounts.Api;

internal static class RegisterEndpoint
{
    public static async Task<IResult> HandleAsync(
        [FromBody] RegisterRequest request, IValidator<RegisterRequest> validator, IRegisterAccount register,
        HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        request = request with { Email = request.Email?.Trim() };
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await register.ExecuteAsync(request.Email!, request.Password!, cancellationToken);
        if (result.Id is Guid id)
        {
            return Results.Created((string?)null, new { id, email = request.Email });
        }

        var failure = result.Failure!;
        if (failure.DuplicateEmail)
        {
            return Results.Problem(title: "An account already exists for this email address.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var errors = new Dictionary<string, string[]>();
        if (failure.EmailError is { } emailError)
        {
            errors.Add("email", [emailError]);
        }
        if (failure.PasswordErrors is { Count: > 0 } passwordErrors)
        {
            errors.Add("password", passwordErrors.ToArray());
        }

        return Results.ValidationProblem(errors);
    }
}
