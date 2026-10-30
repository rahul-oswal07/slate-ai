using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Admin;

public sealed class AdminEndpointTests
{
    private const string Key = "a-test-admin-key-of-32-characters";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly FakeAdminStore _store = new();

    [Fact]
    public async Task The_right_key_lists_every_greeting_and_plays_one()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var list = await client.GetAsync("/api/admin/greetings", Ct);
        var greetings = (await list.Content.ReadFromJsonAsync<List<AdminGreeting>>(Ct))!;
        var audio = await client.GetAsync($"/api/admin/greetings/{_store.Greeting.ClipId}/audio", Ct);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal("Shobha", Assert.Single(greetings).TeacherName);
        Assert.Equal("no-store", list.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.OK, audio.StatusCode);
        Assert.Equal("audio/ogg", audio.Content.Headers.ContentType?.MediaType);
        Assert.Equal("OggS"u8.ToArray(), await audio.Content.ReadAsByteArrayAsync(Ct));
        Assert.Equal("no-store", audio.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a-test-admin-key-of-32-characterX")]
    [InlineData("a-test-admin-key")]
    public async Task A_missing_or_wrong_key_is_404_like_a_route_that_does_not_exist(string? sent)
    {
        await using var factory = Create(Key);
        using var client = Client(factory, sent);

        var list = await client.GetAsync("/api/admin/greetings", Ct);
        var audio = await client.GetAsync($"/api/admin/greetings/{_store.Greeting.ClipId}/audio", Ct);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, audio.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public async Task Without_a_long_enough_key_configured_there_are_no_admin_endpoints(string? configured)
    {
        await using var factory = Create(configured);
        using var client = Client(factory, configured);

        var list = await client.GetAsync("/api/admin/greetings", Ct);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.False(_store.Listed);
    }

    [Fact]
    public async Task A_clip_that_is_not_a_greeting_is_404()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var audio = await client.GetAsync($"/api/admin/greetings/{Guid.NewGuid()}/audio", Ct);

        Assert.Equal(HttpStatusCode.NotFound, audio.StatusCode);
    }

    private static HttpClient Client(ApiFactory factory, string? key)
    {
        var client = factory.CreateHttpsClient();
        if (key is not null)
        {
            client.DefaultRequestHeaders.Add(AdminEndpoints.KeyHeader, key);
        }

        return client;
    }

    private ApiFactory Create(string? key) => FakeApi.Create(
        services => services.AddSingleton<IAdminStore>(_store),
        settings: new Dictionary<string, string?> { ["Admin:Key"] = key });

    /// <summary>One greeting; any other clip ID stands for a clip that isn't a greeting.</summary>
    private sealed class FakeAdminStore : IAdminStore
    {
        public AdminGreeting Greeting { get; } =
            new(Guid.NewGuid(), "Shobha", 8.2, new DateTimeOffset(2026, 10, 28, 9, 0, 0, TimeSpan.Zero));

        public bool Listed { get; private set; }

        public Task<IReadOnlyList<AdminGreeting>> ListGreetingsAsync(CancellationToken cancellationToken)
        {
            Listed = true;
            return Task.FromResult<IReadOnlyList<AdminGreeting>>([Greeting]);
        }

        public Task<AdminClip?> GetGreetingAsync(Guid clipId, CancellationToken cancellationToken) =>
            Task.FromResult(clipId == Greeting.ClipId ? new AdminClip("OggS"u8.ToArray(), "audio/ogg") : null);
    }
}
