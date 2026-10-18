using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>Runs the real API in-process against the given database.</summary>
/// <param name="webRoot">Optional static-file root, so SPA tests don't depend on a real web build.</param>
/// <param name="settings">Extra configuration, e.g. feature switches.</param>
public sealed class ApiFactory(
    string connectionString,
    bool initialiseDatabase = true,
    string? webRoot = null,
    IReadOnlyDictionary<string, string?>? settings = null)
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
    }
}
