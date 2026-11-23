using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManager.Application.Accounts;
using TaskManager.Application.Tasks;

namespace TaskManager.IntegrationTests;

public sealed class ApiExceptionTests(LoginFixture fixture) : IClassFixture<LoginFixture>
{
    private const string Origin = "http://localhost:8088";

    [Fact]
    public async Task DeveloperErrorPageAlsoReturnsASanitizedProblemWhenTheHandlerFails()
    {
        await using var host = fixture.Accounts.WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .ConfigureTestServices(services =>
            {
                services.AddScoped<ISignIn, FailingSignIn>();
                services.RemoveAll<IExceptionHandler>();
                services.AddExceptionHandler<FailingExceptionHandler>();
            }));
        using var client = host.CreateClient();
        ConfigureRequest(client, "text/html");
        using var response = await client.PostAsJsonAsync("/api/accounts/login",
            new { email = "demo@example.test", password = "DemoTasks!2026" });

        await AssertSanitizedProblem(response, "Internal sign-in exception marker.");
    }

    [Theory]
    [InlineData("Development", "text/html")]
    [InlineData("Production", "application/json")]
    public async Task AccountsReturnsASanitizedProblem(string environment, string accept)
    {
        await using var host = fixture.Accounts.WithWebHostBuilder(builder => builder
            .UseEnvironment(environment)
            .ConfigureTestServices(services => services.AddScoped<ISignIn, FailingSignIn>()));
        using var client = host.CreateClient();
        ConfigureRequest(client, accept);
        using var response = await client.PostAsJsonAsync("/api/accounts/login",
            new { email = "demo@example.test", password = "DemoTasks!2026" });

        await AssertSanitizedProblem(response, "Internal sign-in exception marker.");
    }

    [Theory]
    [InlineData("Development", "text/html")]
    [InlineData("Production", "application/json")]
    public async Task TasksReturnsASanitizedProblem(string environment, string accept)
    {
        using var accounts = fixture.Accounts.CreateClient();
        using var login = await accounts.PostAsJsonAsync("/api/accounts/login",
            new { email = "demo@example.test", password = "DemoTasks!2026" });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(token);

        await using var host = fixture.Tasks.WithWebHostBuilder(builder => builder
            .UseEnvironment(environment)
            .ConfigureTestServices(services => services.AddScoped<ITaskQueries, FailingTaskQueries>()));
        using var client = host.CreateClient();
        ConfigureRequest(client, accept);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await client.GetAsync("/api/tasks");

        await AssertSanitizedProblem(response, "Internal task query exception marker.");
    }

    private static void ConfigureRequest(HttpClient client, string accept)
    {
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        client.DefaultRequestHeaders.Add("Origin", Origin);
    }

    private static async Task AssertSanitizedProblem(HttpResponseMessage response, string exceptionMarker)
    {
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.True(response.Headers.CacheControl?.NoStore);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(exceptionMarker, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        using var problem = JsonDocument.Parse(body);
        var value = problem.RootElement;
        Assert.Equal(500, value.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", value.GetProperty("title").GetString());
        Assert.False(value.TryGetProperty("detail", out _));
        Assert.False(string.IsNullOrWhiteSpace(value.GetProperty("traceId").GetString()));
    }
}
