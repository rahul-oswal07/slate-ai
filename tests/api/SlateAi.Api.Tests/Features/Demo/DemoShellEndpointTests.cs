using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>The demo shell's login, Reset, parent phone and "Under the hood" endpoints (T-18), over fakes.</summary>
public sealed class DemoShellEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DemoSeedFile Seed = DemoSeedFile.LoadAsync("data/seed/demo-class.json", Ct).GetAwaiter().GetResult();
    private static readonly Guid ClassId = Seed.Class.Id;
    private static readonly Guid TeacherId = Seed.Teacher.Id;
    private static readonly Guid Rohan = Seed.Children.Single(c => c.RollNo == DemoStory.RohanRollNo).Id;

    // Fri 23 Oct 2026, 10:00 IST.
    private static readonly DateTimeOffset Now = SchoolClock.At(new DateOnly(2026, 10, 23), new TimeOnly(10, 0));

    private readonly FakeDemoStore _store = new();
    private readonly FakeAuthStore _auth = new();

    [Fact]
    public async Task Login_signs_in_the_demo_teacher_without_a_pin()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync("/api/demo/login", null, Ct);
        var me = await client.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("slate.session=", Assert.Single(response.Headers.GetValues("Set-Cookie")));
        Assert.Equal(TeacherId, me!.TeacherId);
        Assert.Equal(ClassId, me.ClassId);
    }

    [Fact]
    public async Task Login_is_404_when_the_demo_teacher_is_not_seeded()
    {
        _auth.Seeded = false;
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync("/api/demo/login", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("POST", "/api/demo/login")]
    [InlineData("POST", "/api/demo/reset")]
    [InlineData("GET", "/api/demo")]
    [InlineData("GET", "/api/demo/ai-calls")]
    public async Task Nothing_is_mapped_unless_demo_is_enabled(string method, string url)
    {
        await using var factory = Create(enabled: false);
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await client.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new DemoResetRequest(Guid.NewGuid())) },
            Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Info_puts_rohans_hindi_speaking_family_on_the_parent_phone_and_gives_the_last_reset()
    {
        _store.LastReset = Now.AddMinutes(-3);
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var info = await client.GetFromJsonAsync<DemoInfoResponse>("/api/demo", Ct);

        Assert.Equal(new DemoInfoResponse(ClassId, Rohan, Now.AddMinutes(-3)), info);
        Assert.Equal("hi", Seed.Children.Single(c => c.Id == Rohan).HomeLanguage);
    }

    [Fact]
    public async Task Reset_writes_the_story_days_without_a_pre_made_catch_up_when_there_is_none()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        var requestId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/demo/reset", new DemoResetRequest(requestId), Ct);
        var body = await response.Content.ReadFromJsonAsync<DemoResetResponse>(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new DemoResetResponse(Now, PreMade: false), body);
        var reset = Assert.Single(_store.Resets);
        Assert.Equal(requestId, reset.ClientRequestId);
        Assert.Equal(new DateOnly(2026, 10, 19), reset.WeekOf);
        Assert.Equal(5, reset.Days.Count);
        Assert.Equal(36, reset.Seed.Children.Count);
        Assert.Null(reset.Snapshot);
        Assert.Null(reset.SnapshotChildId);
    }

    [Theory]
    [InlineData("te", DemoStory.RaviRollNo, 4)] // Mon–Thu this week; Sat 17 Oct is last week
    [InlineData("hi", DemoStory.RohanRollNo, 0)] // away today only, so the snapshot's own missed lessons stand
    public async Task Reset_gives_the_pre_made_catch_up_to_the_story_child_of_its_language(string language, int rollNo, int missed)
    {
        var file = Path.Combine(AppContext.BaseDirectory, $"demo-snapshot-{language}-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(file, $$"""
            { "targetCompetencyIds": ["M3.ADD.CARRY"], "targetKey": "M3.ADD.CARRY", "language": "{{language}}",
              "keyWordsKn": [], "activity": "{}", "checkItem": "{}", "missedCompetencyIds": ["M3.ADD.CARRY"],
              "compressionRationale": "{}", "clips": [] }
            """, Ct);
        try
        {
            await using var factory = Create(snapshotFile: Path.GetFileName(file));
            using var client = factory.CreateTeacherClient(ClassId, TeacherId);

            var body = await (await client.PostAsJsonAsync("/api/demo/reset", new DemoResetRequest(Guid.NewGuid()), Ct))
                .Content.ReadFromJsonAsync<DemoResetResponse>(Ct);

            Assert.True(body!.PreMade);
            var reset = Assert.Single(_store.Resets);
            Assert.Equal(Seed.Children.Single(c => c.RollNo == rollNo).Id, reset.SnapshotChildId);
            Assert.Equal(missed, reset.SnapshotMissed.Count);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Reset_is_only_for_the_demo_teacher()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();

        var notDemo = await other.PostAsJsonAsync("/api/demo/reset", new DemoResetRequest(Guid.NewGuid()), Ct);
        var signedOut = await anonymous.PostAsJsonAsync("/api/demo/reset", new DemoResetRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.NotFound, notDemo.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
        Assert.Empty(_store.Resets);
    }

    [Fact]
    public async Task Family_lists_the_links_sent_to_a_child_of_her_class()
    {
        var sent = new FamilyMessage(FamilyMessage.Evening, new DateOnly(2026, 10, 22), Now, "token-1");
        _store.Families[(ClassId, Rohan)] = [sent];
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        var messages = await client.GetFromJsonAsync<List<FamilyMessage>>($"/api/demo/family/{Rohan}", Ct);
        var notHers = await other.GetAsync($"/api/demo/family/{Rohan}", Ct);

        Assert.Equal([sent], messages);
        Assert.Equal(HttpStatusCode.NotFound, notHers.StatusCode);
    }

    [Fact]
    public async Task Ai_calls_pass_since_and_the_panel_limit()
    {
        var call = new AiCallRow(Guid.NewGuid(), Now, "a3-script", "claude-opus-5-5", 2, 900, 400, 0, 5200, "ready", null);
        _store.AiCalls.Add(call);
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        var since = Now.AddMinutes(-5);

        var calls = await client.GetFromJsonAsync<List<AiCallRow>>(
            $"/api/demo/ai-calls?since={Uri.EscapeDataString(since.ToString("O"))}", Ct);

        Assert.Equal([call], calls);
        Assert.Equal((since, DemoEndpoints.AiCallLimit), _store.LastAiCallQuery);
    }

    [Fact]
    public async Task Snapshot_export_is_development_only()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await client.GetAsync($"/api/demo/snapshot/{Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(_store.Exported);
    }

    private ApiFactory Create(bool enabled = true, string snapshotFile = "no-such-snapshot.json") =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<IDemoStore>(_store);
                services.AddSingleton<IAuthStore>(_auth);
                services.AddSingleton<TimeProvider>(new FixedClock(Now));
            },
            settings: new Dictionary<string, string?>
            {
                ["Demo:Enabled"] = enabled.ToString(),
                ["Demo:SnapshotFile"] = snapshotFile,
            });

    private sealed class FakeDemoStore : IDemoStore
    {
        public List<DemoReset> Resets { get; } = [];
        public Dictionary<(Guid ClassId, Guid ChildId), IReadOnlyList<FamilyMessage>> Families { get; } = [];
        public List<AiCallRow> AiCalls { get; } = [];
        public (DateTimeOffset? Since, int Limit)? LastAiCallQuery { get; private set; }
        public bool Exported { get; private set; }
        public DateTimeOffset? LastReset { get; set; }

        public Task<DateTimeOffset?> GetLastResetAsync(CancellationToken cancellationToken) => Task.FromResult(LastReset);

        public Task<DemoResetResult> ResetAsync(DemoReset reset, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Resets.Add(reset);
            return Task.FromResult(DemoResetResult.Reset);
        }

        public Task<IReadOnlyList<FamilyMessage>?> GetFamilyAsync(Guid classId, Guid childId, CancellationToken cancellationToken) =>
            Task.FromResult(Families.GetValueOrDefault((classId, childId)));

        public Task<IReadOnlyList<AiCallRow>> GetAiCallsAsync(DateTimeOffset? since, int limit, CancellationToken cancellationToken)
        {
            LastAiCallQuery = (since, limit);
            return Task.FromResult<IReadOnlyList<AiCallRow>>(AiCalls);
        }

        public Task<DemoSnapshot?> ExportAsync(Guid classId, Guid packageId, CancellationToken cancellationToken)
        {
            Exported = true;
            return Task.FromResult<DemoSnapshot?>(null);
        }
    }

    private sealed class FakeAuthStore : IAuthStore
    {
        public bool Seeded { get; set; } = true;

        public Task<TeacherLogin?> FindByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult<TeacherLogin?>(null);

        public Task SetPinFailuresAsync(
            Guid teacherId, int failedAttempts, DateTimeOffset? lockedUntil, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<MeResponse?> GetMeAsync(Guid teacherId, CancellationToken cancellationToken) =>
            Task.FromResult(Seeded && teacherId == TeacherId ? new MeResponse(TeacherId, "Shobha", ClassId, true) : null);

        public Task<MeResponse?> MarkWelcomedAsync(Guid teacherId, DateTimeOffset at, CancellationToken cancellationToken) =>
            GetMeAsync(teacherId, cancellationToken);
    }
}
