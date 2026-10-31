using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Tests.Features.Attendance;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>The demo video story (T-70, D-85): the Reset's week, its evening messages and the parent phone, over fakes.</summary>
public sealed class DemoVideoStoryEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DemoSeedFile Seed = DemoSeedFile.LoadAsync("data/seed/demo-class.json", Ct).GetAwaiter().GetResult();
    private static readonly Guid ClassId = Seed.Class.Id;
    private static readonly Guid TeacherId = Seed.Teacher.Id;

    // Sat 31 Oct 2026, 10:00 IST: the week Mon 26 – Fri 30 Oct is behind us.
    private static readonly DateOnly Today = new(2026, 10, 31);
    private static readonly DateTimeOffset Now = SchoolClock.At(Today, new TimeOnly(10, 0));

    private static readonly EveningWording Wording = new("1", new Dictionary<string, LanguageWording>
    {
        ["en"] = new("Today {child} … {lesson} … {page}", "part {part}, page {page}", "page {page}"),
    });

    private static readonly CurriculumGraph Book = new(
        DemoStory.Lessons.Select((id, i) =>
            new Competency(id, $"ಪಾಠ {i}", $"Lesson {i}", [], null, 1, i, [], new TextbookPage("textbook", 1, 10 + i))).ToList(),
        []);

    private static readonly int[] StoryRolls = [DemoStory.AnanyaRollNo, DemoStory.RaviRollNo, DemoStory.RohanRollNo, DemoStory.ArunRollNo];

    private readonly FakeEveningStore _evening = new(ClassId, TeacherId);
    private readonly FakeAttendanceStore _attendance;
    private readonly FakeDemoStore _demo;

    public DemoVideoStoryEndpointTests()
    {
        // The four story children and one who is never away; all "en" here, so one template covers them.
        var children = Seed.Children.Where(c => StoryRolls.Contains(c.RollNo) || c.RollNo == 2).ToList();
        _evening.Children.AddRange(children.Select(c => new EveningChild(c.Id, c.RollNo, c.FirstName, "en", "yes", false)));
        _attendance = new FakeAttendanceStore(ClassId, children.Select(c => c.Id).ToList());
        _demo = new FakeDemoStore(_attendance, _evening);
    }

    [Fact]
    public async Task Reset_writes_the_week_and_each_evenings_messages_at_6_pm()
    {
        Approve();

        var body = await ResetAsync();

        var reset = Assert.Single(_demo.Resets);
        Assert.Null(reset.Snapshot);
        Assert.Equal(
            [new DateOnly(2026, 10, 26), new(2026, 10, 27), new(2026, 10, 28), new(2026, 10, 29), new(2026, 10, 30)],
            reset.Days.Select(d => d.Date));
        Assert.Equal(10, body.EveningSent);

        var sent = _evening.Messages.GroupBy(m => Roll(m.Message.ChildId)).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(
            new Dictionary<int, int> { [DemoStory.AnanyaRollNo] = 5, [DemoStory.RaviRollNo] = 2, [DemoStory.RohanRollNo] = 1, [DemoStory.ArunRollNo] = 2 },
            sent);
        Assert.All(_evening.Messages, m =>
        {
            Assert.Equal(SchoolClock.At(m.Date, new TimeOnly(18, 0)), m.Message.SentAt);
            Assert.Equal(m.Message.SentAt.AddDays(14), m.Message.ExpiresAt);
        });
    }

    [Fact]
    public async Task Nothing_goes_while_the_evening_template_is_not_approved()
    {
        var body = await ResetAsync();

        Assert.Equal(5, Assert.Single(_demo.Resets).Days.Count);
        Assert.Equal(0, body.EveningSent);
        Assert.Empty(_evening.Messages);
    }

    [Fact]
    public async Task Info_puts_ananyas_kannada_family_on_the_parent_phone()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var info = await client.GetFromJsonAsync<DemoInfoResponse>("/api/demo", Ct);

        var ananya = Seed.Children.Single(c => c.Id == info!.ParentChildId);
        Assert.Equal((DemoStory.AnanyaRollNo, "kn"), (ananya.RollNo, ananya.HomeLanguage));
        Assert.Equal(DemoOptions.VideoStory, info!.Story);
        Assert.Null(info.FixedToday);
    }

    [Fact]
    public async Task Info_says_when_the_demo_date_is_fixed()
    {
        await using var factory = Create(new Dictionary<string, string?> { ["Demo:Today"] = "2026-10-31" });
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var info = await client.GetFromJsonAsync<DemoInfoResponse>("/api/demo", Ct);

        Assert.Equal(new DateOnly(2026, 10, 31), info!.FixedToday);
    }

    [Fact]
    public void The_story_setting_switches_the_feed_to_the_video_scenario()
    {
        using var factory = Create();

        var sats = factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<SatsOptions>>().Value;

        Assert.Equal(DemoEndpoints.VideoScenarioFile, sats.ScenarioFile);
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, sats.ScenarioFile)));
    }

    private static int Roll(Guid childId) => Seed.Children.Single(c => c.Id == childId).RollNo;

    private void Approve()
    {
        _evening.GetTemplatesAsync(ClassId, Wording, Ct).GetAwaiter().GetResult();
        _evening.ApproveAsync(ClassId, Guid.NewGuid(), Wording.Version, TeacherId, Ct).GetAwaiter().GetResult();
    }

    private async Task<DemoResetResponse> ResetAsync()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        var response = await client.PostAsJsonAsync("/api/demo/reset", new DemoResetRequest(Guid.NewGuid()), Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DemoResetResponse>(Ct))!;
    }

    private ApiFactory Create(IReadOnlyDictionary<string, string?>? extra = null) =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<IDemoStore>(_demo);
                services.AddSingleton<IEveningStore>(_evening);
                services.AddSingleton<IAttendanceStore>(_attendance);
                services.AddSingleton(Book);
                services.AddSingleton(Wording);
                services.AddSingleton<TimeProvider>(new FixedClock(Now));
            },
            settings: new Dictionary<string, string?>(extra ?? new Dictionary<string, string?>())
            {
                ["Demo:Enabled"] = "true",
                ["Demo:Story"] = DemoOptions.VideoStory,
            });

    /// <summary>Writes what the real Reset writes that the evening job reads: each day's register and confirmed lesson.</summary>
    private sealed class FakeDemoStore(FakeAttendanceStore attendance, FakeEveningStore evening) : IDemoStore
    {
        public List<DemoReset> Resets { get; } = [];

        public Task<DemoResetResult> ResetAsync(DemoReset reset, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Resets.Add(reset);
            foreach (var day in reset.Days)
            {
                attendance.SetDay(day.Date, evening.Children
                    .Select(c => new AttendanceRow(c.Id, !day.AbsentChildIds.Contains(c.Id), AttendanceSources.Sats, null, null, null))
                    .ToList());
                evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), day.Date, [day.CompetencyId]));
            }

            return Task.FromResult(DemoResetResult.Reset);
        }

        public Task<IReadOnlyList<FamilyMessage>?> GetFamilyAsync(Guid classId, Guid childId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FamilyMessage>?>([]);

        public Task<DateTimeOffset?> GetLastResetAsync(CancellationToken cancellationToken) => Task.FromResult<DateTimeOffset?>(null);

        public Task<IReadOnlyList<AiCallRow>> GetAiCallsAsync(DateTimeOffset? since, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AiCallRow>>([]);

        public Task<DemoSnapshot?> ExportAsync(Guid classId, Guid packageId, CancellationToken cancellationToken) =>
            Task.FromResult<DemoSnapshot?>(null);
    }
}
