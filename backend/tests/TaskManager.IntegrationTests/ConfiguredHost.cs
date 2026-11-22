using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TaskManager.IntegrationTests;

internal sealed class ConfiguredHost<THost>(Dictionary<string, string?> settings)
    : WebApplicationFactory<THost> where THost : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings));
    }
}
