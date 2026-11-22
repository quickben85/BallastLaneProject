using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.IntegrationTests;

public sealed class LoginApiTests(LoginFixture fixture) : IClassFixture<LoginFixture>
{
    [Theory]
    [InlineData("http://localhost:8088", true)]
    [InlineData("http://127.0.0.1:8088", true)]
    [InlineData("http://localhost:4200", true)]
    [InlineData("http://127.0.0.1:4200", true)]
    [InlineData("https://untrusted.example.test", false)]
    public async Task BothApisAllowOnlyConfiguredBrowserOrigins(string origin, bool allowed)
    {
        using var accounts = fixture.Accounts.CreateClient();
        using var tasks = fixture.Tasks.CreateClient();
        await AssertPreflight(accounts, "/api/accounts/login", "POST", "content-type");
        await AssertPreflight(tasks, "/api/tasks", "GET", "authorization");

        async Task AssertPreflight(HttpClient client, string path, string method, string header)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, path);
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", method);
            request.Headers.Add("Access-Control-Request-Headers", header);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            if (allowed)
            {
                Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
                Assert.Contains(header, response.Headers.GetValues("Access-Control-Allow-Headers"),
                    StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
            }
        }
    }

    [Fact]
    public async Task EmptyCredentialsReturnFieldErrors()
    {
        using var client = fixture.Accounts.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/accounts/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("email", problem.Errors.Keys);
        Assert.Contains("password", problem.Errors.Keys);
    }

    [Theory]
    [InlineData("""{"email":100,"password":"example"}""", "email")]
    [InlineData("""{"Email":"demo@example.test","Password":100}""", "password")]
    [InlineData("null", "body")]
    [InlineData("{", "body")]
    [InlineData("", "body")]
    public async Task MalformedRequestReturnsValidationProblem(string json, string field)
    {
        const string origin = "http://localhost:8088";
        using var client = fixture.Accounts.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", origin);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/accounts/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.True(response.Headers.CacheControl?.NoStore);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(field, problem.Errors.Keys);
    }

    [Fact]
    public async Task WrongContentTypeReturns415()
    {
        using var client = fixture.Accounts.CreateClient();
        using var content = new StringContent("email=demo@example.test", Encoding.UTF8, "text/plain");
        using var response = await client.PostAsync("/api/accounts/login", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task DemoAccountCanSignInAndTokenAuthenticatesWithTasksHost()
    {
        using var client = fixture.Accounts.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/accounts/login",
            new { email = " DEMO@example.test ", password = "DemoTasks!2026" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.Equal(900, login.ExpiresInSeconds);

        await using var scope = fixture.Tasks.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Headers.Authorization = $"Bearer {login.AccessToken}";
        var result = await context.AuthenticateAsync("Bearer");

        Assert.True(result.Succeeded, result.Failure?.Message);
        Assert.Equal("ea9dd2dc-5959-4ca7-b98d-a38923c31aff", result.Principal?.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task CorrectCredentialsAreAcceptedAfterRepeatedFailures()
    {
        const string email = "retry@example.test";
        const string password = "TestRetry!2026";
        await using (var scope = fixture.Accounts.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = email };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
        }

        using var client = fixture.Accounts.CreateClient();
        await AssertRejected("missing@example.test", password);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await AssertRejected(email, "wrong-password");
        }

        using var success = await client.PostAsJsonAsync("/api/accounts/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);

        async Task AssertRejected(string address, string submittedPassword)
        {
            using var response = await client.PostAsJsonAsync("/api/accounts/login",
                new { email = address, password = submittedPassword });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal("Invalid email or password.", problem?.Title);
        }
    }
}
