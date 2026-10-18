using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>Runs the real API in-process against the given database.</summary>
public sealed class ApiFactory(string connectionString, bool initialiseDatabase = true)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Db"] = connectionString,
            ["Database:MigrateOnStartup"] = initialiseDatabase.ToString(),
        }));
    }
}
