using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Accounts;

namespace TaskManager.Accounts.Api;

internal static class LoginEndpoint
{
    public static async Task<IResult> HandleAsync(
        [FromBody] LoginRequest request, IValidator<LoginRequest> validator, ISignIn signIn,
        HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        request = request with { Email = request.Email?.Trim() };
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var token = await signIn.ExecuteAsync(request.Email!, request.Password!, cancellationToken);
        return token is null
            ? Results.Problem(title: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized)
            : Results.Ok(new { accessToken = token.Value, expiresInSeconds = token.ExpiresInSeconds });
    }
}
