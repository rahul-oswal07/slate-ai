using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Tests.Features.Demo;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Admin;

/// <summary>The admin switch for acting as a past day in the demo class (T-134, D-120): off until switched on.</summary>
public sealed class AdminDemoEndpointTests
{
    private const string Url = "/api/admin/demo";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 5, 0, 0, TimeSpan.Zero);

    private readonly FakeSchoolDateSwitchStore _store = new();

    [Fact]
    public async Task Starts_off_and_switches_on_and_off()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var before = await client.GetAsync(Url, Ct);
        var on = await client.PutAsJsonAsync($"{Url}/school-date", new SetSchoolDateSwitchRequest(Guid.NewGuid(), true), Ct);
        var after = await client.GetFromJsonAsync<AdminDemo>(Url, Ct);
        var off = await client.PutAsJsonAsync($"{Url}/school-date", new SetSchoolDateSwitchRequest(Guid.NewGuid(), false), Ct);

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.Equal("no-store", before.Headers.CacheControl?.ToString());
        Assert.Equal(new AdminDemo(true, false, null), await before.Content.ReadFromJsonAsync<AdminDemo>(Ct));
        Assert.Equal(new AdminDemo(true, true, Now), await on.Content.ReadFromJsonAsync<AdminDemo>(Ct));
        Assert.Equal(new AdminDemo(true, true, Now), after);
        Assert.Equal(new AdminDemo(true, false, Now), await off.Content.ReadFromJsonAsync<AdminDemo>(Ct));
    }

    [Fact]
    public async Task A_replayed_request_applies_once()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var request = new SetSchoolDateSwitchRequest(Guid.NewGuid(), true);

        await client.PutAsJsonAsync($"{Url}/school-date", request, Ct);
        var replay = await client.PutAsJsonAsync($"{Url}/school-date", request, Ct);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(1, _store.Writes);
    }

    [Fact]
    public async Task Switching_without_a_client_request_id_is_400()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/school-date", new SetSchoolDateSwitchRequest(Guid.Empty, true), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _store.Writes);
    }

    [Fact]
    public async Task With_the_demo_off_there_is_nothing_to_switch()
    {
        await using var factory = Create(demoEnabled: false);
        using var client = factory.CreateHttpsClient();

        var state = await client.GetFromJsonAsync<AdminDemo>(Url, Ct);
        var response = await client.PutAsJsonAsync($"{Url}/school-date", new SetSchoolDateSwitchRequest(Guid.NewGuid(), true), Ct);

        Assert.Equal(new AdminDemo(false, false, null), state);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, _store.Writes);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Development")]
    public async Task Opens_without_a_key_in_every_environment_and_is_not_indexed_D112(string environment)
    {
        await using var factory = Create(environment: environment);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(Url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("noindex", Assert.Single(response.Headers.GetValues("X-Robots-Tag")));
    }

    private ApiFactory Create(bool demoEnabled = true, string environment = "Testing") => FakeApi.Create(
        services =>
        {
            services.AddSingleton<ISchoolDateSwitchStore>(_store);
            services.AddSingleton<TimeProvider>(new FixedClock(Now));
        },
        settings: new Dictionary<string, string?> { ["Demo:Enabled"] = demoEnabled.ToString() },
        environment: environment);
}
