using System.Net;
using System.Text.Json;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Health;

[Trait("Category", "Integration")]
public sealed class HealthEndpointTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Health_returns_200_when_database_is_reachable()
    {
        await using var factory = new ApiFactory(postgres.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.Equal("dev", body.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Health_reports_the_configured_build_version()
    {
        // The deploy smoke test relies on this to know the new container is the one answering.
        var settings = new Dictionary<string, string?> { ["Build:Version"] = "sha-test123" };
        await using var factory = new ApiFactory(postgres.ConnectionString, settings: settings);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal("sha-test123", (await ReadBodyAsync(response)).GetProperty("version").GetString());
    }

    [Fact]
    public async Task Health_returns_503_when_database_is_unreachable()
    {
        const string unreachable = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2";
        await using var factory = new ApiFactory(unreachable, initialiseDatabase: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
