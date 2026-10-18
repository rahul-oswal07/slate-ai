using System.Net;
using System.Net.Http.Json;
using SlateAi.Api.Features.Health;
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
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Healthy", body?.Status);
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
}
