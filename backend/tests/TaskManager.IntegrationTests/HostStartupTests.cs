using System.Net;
using AccountsHost = TaskManager.Accounts.Api.Program;
using TasksHost = TaskManager.Tasks.Api.Program;

namespace TaskManager.IntegrationTests;

public sealed class HostStartupTests
{
    [Fact]
    public Task AccountsHostRespondsToLivenessProbe() => AssertLive<AccountsHost>();

    [Fact]
    public Task TasksHostRespondsToLivenessProbe() => AssertLive<TasksHost>();

    private static async Task AssertLive<THost>() where THost : class
    {
        await using var factory = new SmokeTestHost<THost>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/_internal/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
