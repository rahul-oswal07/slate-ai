using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Admin;

public sealed class AdminEndpointTests
{
    private const string Key = "a-test-admin-key-of-32-characters";
    private const string Day = "2026-10-29";
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

        string[] urls =
        [
            "/api/admin/greetings",
            $"/api/admin/greetings/{_store.Greeting.ClipId}/audio",
            $"/api/admin/lesson-audio?date={Day}",
            $"/api/admin/lesson-audio/{FakeAdminStore.LessonClipId}/audio",
        ];
        foreach (var url in urls)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url, Ct)).StatusCode);
        }

        Assert.False(_store.Read);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public async Task Without_a_long_enough_key_configured_there_are_no_admin_endpoints(string? configured)
    {
        await using var factory = Create(configured);
        using var client = Client(factory, configured);

        var list = await client.GetAsync("/api/admin/greetings", Ct);
        var day = await client.GetAsync($"/api/admin/lesson-audio?date={Day}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, day.StatusCode);
        Assert.False(_store.Read);
    }

    [Fact]
    public async Task Each_audio_route_serves_only_its_own_kind_of_clip()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var lessonAsGreeting = await client.GetAsync($"/api/admin/greetings/{FakeAdminStore.LessonClipId}/audio", Ct);
        var greetingAsLesson = await client.GetAsync($"/api/admin/lesson-audio/{_store.Greeting.ClipId}/audio", Ct);
        var lesson = await client.GetAsync($"/api/admin/lesson-audio/{FakeAdminStore.LessonClipId}/audio", Ct);
        var unknown = await client.GetAsync($"/api/admin/greetings/{Guid.NewGuid()}/audio", Ct);

        Assert.Equal(HttpStatusCode.NotFound, lessonAsGreeting.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, greetingAsLesson.StatusCode);
        Assert.Equal(HttpStatusCode.OK, lesson.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task The_day_view_counts_sends_and_names_lessons_families_and_unsent_audio()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var response = await client.GetAsync($"/api/admin/lesson-audio?date={Day}", Ct);
        var day = (await response.Content.ReadFromJsonAsync<AdminLessonAudioDay>(Ct))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(new DateOnly(2026, 10, 29), _store.AskedFor);
        Assert.Equal(2, day.SentCount);
        Assert.Equal(2, day.Audios.Count);

        var sent = day.Audios[0];
        Assert.Equal("Shobha · Grade 3", sent.ClassLabel);
        Assert.Equal(["3-digit addition with carrying", "M9.UNKNOWN"], sent.Lessons); // an unknown ID shows as itself
        Assert.Equal("Savitha", sent.ApprovedBy);
        Assert.Equal(FakeAdminStore.LessonClipId, sent.ClipId);
        Assert.Equal(["Ravi", "Rohan"], sent.Families.Select(f => f.ChildName));

        var failed = day.Audios[1];
        Assert.Equal("failed", failed.Status);
        Assert.Equal("fallback:numbers", failed.Verdict);
        Assert.Null(failed.ClipId);
        Assert.Empty(failed.Families);
    }

    [Theory]
    [InlineData("")]
    [InlineData("29-10-2026")]
    [InlineData("2026-13-01")]
    public async Task A_missing_or_invalid_date_is_400(string date)
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var missing = await client.GetAsync("/api/admin/lesson-audio", Ct);
        var invalid = await client.GetAsync($"/api/admin/lesson-audio?date={date}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Null(_store.AskedFor);
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

    /// <summary>One greeting, one lesson clip and one day with a sent and a failed lesson audio.</summary>
    private sealed class FakeAdminStore : IAdminStore
    {
        public static readonly Guid LessonClipId = Guid.NewGuid();
        private static readonly DateTimeOffset SixPm = new(2026, 10, 29, 12, 30, 0, TimeSpan.Zero);

        public AdminGreeting Greeting { get; } =
            new(Guid.NewGuid(), "Shobha", 8.2, new DateTimeOffset(2026, 10, 28, 9, 0, 0, TimeSpan.Zero));

        public bool Read { get; private set; }
        public DateOnly? AskedFor { get; private set; }

        public Task<IReadOnlyList<AdminGreeting>> ListGreetingsAsync(CancellationToken cancellationToken)
        {
            Read = true;
            return Task.FromResult<IReadOnlyList<AdminGreeting>>([Greeting]);
        }

        public Task<AdminClip?> GetClipAsync(Guid clipId, string kind, CancellationToken cancellationToken)
        {
            Read = true;
            var found = (clipId == Greeting.ClipId && kind == AudioKind.Greeting)
                || (clipId == LessonClipId && kind == AudioKind.Lesson);
            return Task.FromResult(found ? new AdminClip("OggS"u8.ToArray(), "audio/ogg") : null);
        }

        public Task<IReadOnlyList<AdminLessonAudioRow>> GetLessonAudioDayAsync(DateOnly date, CancellationToken cancellationToken)
        {
            Read = true;
            AskedFor = date;
            return Task.FromResult<IReadOnlyList<AdminLessonAudioRow>>(
            [
                new(Guid.NewGuid(), "Shobha", 3, "te", ["M3.ADD.CARRY", "M9.UNKNOWN"], "approved", "valid", "Savitha", SixPm,
                    LessonClipId, 42.5, "Today we added with carrying.",
                    [new("Ravi", 4, "te", SixPm), new("Rohan", 9, "te", SixPm)]),
                new(Guid.NewGuid(), "Shobha", 3, "hi", ["M3.ADD.CARRY"], "failed", "fallback:numbers", null, null,
                    null, null, null, []),
            ]);
        }

        public Task<int> CountLessonAudioSentAsync(DateOnly date, CancellationToken cancellationToken) => Task.FromResult(2);
    }
}
