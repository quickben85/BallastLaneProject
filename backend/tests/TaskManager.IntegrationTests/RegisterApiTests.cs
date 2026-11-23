using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.IntegrationTests;

public sealed class RegisterApiTests(LoginFixture fixture) : IClassFixture<LoginFixture>
{
    [Fact]
    public async Task RegistersAnAccountThatCanSignInAndStartsWithNoTasks()
    {
        const string email = "new'user+tasks@example.test";
        const string password = "FirstAccount!2026";
        using var accounts = fixture.Accounts.CreateClient();
        using var response = await accounts.PostAsJsonAsync("/api/accounts/register", new
        {
            email = $"  {email}  ",
            password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = result.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(email, result.GetProperty("email").GetString());
        Assert.False(result.TryGetProperty("password", out _));
        Assert.False(result.TryGetProperty("passwordHash", out _));

        await using var scope = fixture.Accounts.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var persisted = await users.FindByNameAsync(email);
        Assert.NotNull(persisted);
        Assert.Equal(id, persisted.Id);
        Assert.False(string.IsNullOrWhiteSpace(persisted.PasswordHash));
        Assert.NotEqual(password, persisted.PasswordHash);

        using var signIn = await accounts.PostAsJsonAsync("/api/accounts/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        var token = await signIn.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(token);
        using var tasks = fixture.Tasks.CreateClient();
        tasks.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        Assert.Empty((await tasks.GetFromJsonAsync<TaskResponse[]>("/api/tasks"))!);
    }

    [Fact]
    public async Task ACaseInsensitiveDuplicateDoesNotReplaceTheOriginalCredentials()
    {
        const string email = "duplicate@example.test";
        const string password = "Original!2026";
        const string replacement = "Replacement!2026";
        using var client = fixture.Accounts.CreateClient();
        using var created = await client.PostAsJsonAsync("/api/accounts/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var duplicate = await client.PostAsJsonAsync("/api/accounts/register",
            new { email = $" {email.ToUpperInvariant()} ", password = replacement });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var originalLogin = await client.PostAsJsonAsync("/api/accounts/login", new { email, password });
        using var replacementLogin = await client.PostAsJsonAsync("/api/accounts/login",
            new { email, password = replacement });
        Assert.Equal(HttpStatusCode.OK, originalLogin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementLogin.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRegistrationsCreateOnlyOneAccount()
    {
        using var client = fixture.Accounts.CreateClient();
        var first = client.PostAsJsonAsync("/api/accounts/register",
            new { email = "race@example.test", password = "FirstAccount!2026" });
        var second = client.PostAsJsonAsync("/api/accounts/register",
            new { email = "RACE@example.test", password = "SecondAccount!2026" });

        await Task.WhenAll(first, second);
        using var firstResponse = await first;
        using var secondResponse = await second;
        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
            new[] { firstResponse.StatusCode, secondResponse.StatusCode }.Order());
    }

    [Theory]
    [InlineData("bad-email", "ValidPassword!2026", "email")]
    [InlineData("josé@example.test", "ValidPassword!2026", "email")]
    [InlineData("\"quoted\"@example.test", "ValidPassword!2026", "email")]
    [InlineData("local@-example.test", "ValidPassword!2026", "email")]
    [InlineData("local@in_valid.test", "ValidPassword!2026", "email")]
    [InlineData("weak-password@example.test", "weak", "password")]
    [InlineData("empty-password@example.test", "", "password")]
    [InlineData("", "ValidPassword!2026", "email")]
    public async Task InvalidRegistrationReturnsAFieldError(string email, string password, string field)
    {
        using var client = fixture.Accounts.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/accounts/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(detail);
        Assert.Contains(field, detail.Errors.Keys);
    }
}
