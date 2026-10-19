using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>Runs the real API in-process against the given database.</summary>
/// <param name="webRoot">Optional static-file root, so SPA tests don't depend on a real web build.</param>
/// <param name="settings">Extra configuration, e.g. feature switches.</param>
/// <param name="services">Service overrides, e.g. fake stores so a test needs no database.</param>
public sealed class ApiFactory(
    string connectionString,
    bool initialiseDatabase = true,
    string? webRoot = null,
    IReadOnlyDictionary<string, string?>? settings = null,
    Action<IServiceCollection>? services = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        if (webRoot is not null)
        {
            builder.UseWebRoot(webRoot);
        }

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Db"] = connectionString,
                ["Database:MigrateOnStartup"] = initialiseDatabase.ToString(),
            });
            if (settings is not null)
            {
                config.AddInMemoryCollection(settings);
            }
        });

        if (services is not null)
        {
            builder.ConfigureTestServices(services);
        }
    }
}
