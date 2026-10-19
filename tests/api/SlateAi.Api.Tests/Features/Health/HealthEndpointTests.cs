using System.Net;
using System.Text.Json;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Health;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Health_returns_503_when_database_is_unreachable()
    {
        await using var factory = FakeApi.Create();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", (await ReadBodyAsync(response)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_reports_the_configured_build_version()
    {
        // The deploy smoke test relies on this to know the new container is the one answering.
        var settings = new Dictionary<string, string?> { ["Build:Version"] = "sha-test123" };
        await using var factory = FakeApi.Create(settings: settings);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal("sha-test123", (await ReadBodyAsync(response)).GetProperty("version").GetString());
    }

    [Fact]
    public async Task Health_reports_dev_when_no_version_is_configured()
    {
        await using var factory = FakeApi.Create();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal("dev", (await ReadBodyAsync(response)).GetProperty("version").GetString());
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
