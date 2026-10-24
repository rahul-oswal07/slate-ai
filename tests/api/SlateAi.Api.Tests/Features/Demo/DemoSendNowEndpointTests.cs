using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Tests.Features.Attendance;
using SlateAi.Api.Tests.Features.CatchUps;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Features.Parent;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>"Send messages now" on /demo (T-18): the 6 pm job for the demo class, on a tap, over fakes.</summary>
public sealed class DemoSendNowEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DemoSeedFile Seed = DemoSeedFile.LoadAsync("data/seed/demo-class.json", Ct).GetAwaiter().GetResult();
    private static readonly Guid ClassId = Seed.Class.Id;
    private static readonly Guid TeacherId = Seed.Teacher.Id;

    // Fri 23 Oct 2026, 11:00 IST: well before the 6 pm send.
    private static readonly DateOnly Today = new(2026, 10, 23);

    private static readonly EveningWording Wording = new("1", new Dictionary<string, LanguageWording>
    {
        ["en"] = new("Today {child} … {lesson} … {page}", "part {part}, page {page}", "page {page}"),
    });

    private static readonly CurriculumGraph Book = new(
        [
            new Competency("M3.SUB.BORROW", "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ", "Subtraction with borrowing", [], null, 5, 4, [],
                new TextbookPage("textbook", 2, 15)),
        ],
        []);

    private static readonly EveningChild Rohan = new(Guid.NewGuid(), 24, "Rohan", "en", "yes", false);
    private static readonly EveningChild Present = new(Guid.NewGuid(), 1, "Ananya", "en", "yes", false);

    private readonly MovableClock _clock = new() { Now = SchoolClock.At(Today, new TimeOnly(11, 0)) };
    private readonly FakeEveningStore _evening = new(ClassId, TeacherId);
    private readonly FakeAttendanceStore _attendance = new(ClassId, [Rohan.Id, Present.Id]);
    private readonly FakeCatchUpStore _catchUps;

    public DemoSendNowEndpointTests()
    {
        _evening.Children.AddRange([Rohan, Present]);
        _attendance.SetDay(Today, [
            new AttendanceRow(Rohan.Id, false, AttendanceSources.Sats, null, null, null),
            new AttendanceRow(Present.Id, true, AttendanceSources.Sats, null, null, null),
        ]);
        _evening.GetTemplatesAsync(ClassId, Wording, Ct).GetAwaiter().GetResult();
        _evening.ApproveAsync(ClassId, Guid.NewGuid(), Wording.Version, TeacherId, Ct).GetAwaiter().GetResult();
        _evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today, ["M3.SUB.BORROW"]));

        _catchUps = new FakeCatchUpStore(ClassId, _clock);
        _catchUps.Families[Rohan.Id] = ("Rohan", "yes", false);
        var rationale = """{"targets":[{"id":"M3.SUB.BORROW","reason":"Next week needs it."}],"dropped":[],"source":"ai","verdict":"valid"}""";
        _catchUps.SaveGenerationAsync(ClassId, CompressionPlanner.Week(Today).From,
            [new PlannedPackage(["M3.SUB.BORROW"], "M3.SUB.BORROW", "en", PackagePractice.For(["M3.SUB.BORROW"], A3ValidatorTests.Book, "seed"))],
            [new PlannedAssignment(Rohan.Id, "M3.SUB.BORROW", "en", ["M3.SUB.BORROW"], rationale)],
            Ct).GetAwaiter().GetResult();
        var package = _catchUps.Packages.Single();
        package.Status = PackageStatus.Approved;
        package.ScriptVersion = 1;
        package.ApprovedScriptVersion = 1;
    }

    [Fact]
    public async Task Sends_tonights_message_and_the_approved_catch_up_before_6_pm()
    {
        var result = await SendNowAsync();

        Assert.Equal(new DemoSendNowResponse(null, 1, 1), result);
        Assert.Equal(Rohan.Id, Assert.Single(_evening.Messages).Message.ChildId);
        Assert.Equal(PackageStatus.Sent, _catchUps.Packages.Single().Status);
    }

    [Fact]
    public async Task A_second_tap_sends_nothing_new()
    {
        await SendNowAsync();

        var again = await SendNowAsync();

        Assert.Equal(new DemoSendNowResponse(null, 0, 0), again);
        Assert.Single(_evening.Messages);
    }

    [Fact]
    public async Task An_unapproved_catch_up_never_goes()
    {
        _catchUps.Packages.Single().Status = PackageStatus.Ready;
        _catchUps.Packages.Single().ApprovedScriptVersion = null;

        var result = await SendNowAsync();

        Assert.Equal(0, result.CatchUpsSent);
        Assert.Equal(PackageStatus.Ready, _catchUps.Packages.Single().Status);
    }

    [Fact]
    public async Task Says_why_tonights_message_did_not_go_and_still_sends_the_catch_up()
    {
        _evening.Lessons.Clear();

        var result = await SendNowAsync();

        Assert.Equal(new DemoSendNowResponse(EveningBlock.LessonNotConfirmed, 0, 1), result);
        Assert.Empty(_evening.Messages);
    }

    [Fact]
    public async Task Another_class_gets_404_and_nothing_is_sent()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/demo/send-now", new DemoSendNowRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(_evening.Messages);
        Assert.Equal(PackageStatus.Approved, _catchUps.Packages.Single().Status);
    }

    [Fact]
    public async Task Is_not_mapped_unless_demo_is_enabled()
    {
        await using var factory = Create(enabled: false);
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await client.PostAsJsonAsync("/api/demo/send-now", new DemoSendNowRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(_evening.Messages);
    }

    private async Task<DemoSendNowResponse> SendNowAsync()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        var response = await client.PostAsJsonAsync("/api/demo/send-now", new DemoSendNowRequest(Guid.NewGuid()), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DemoSendNowResponse>(Ct))!;
    }

    private ApiFactory Create(bool enabled = true) =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<IEveningStore>(_evening);
                services.AddSingleton<ICatchUpStore>(_catchUps);
                services.AddSingleton<IParentStore>(new FakeParentStore());
                services.AddSingleton<IAttendanceStore>(_attendance);
                services.AddSingleton<IAttendanceSource>(new NotYetSource());
                services.AddSingleton(Book);
                services.AddSingleton(Wording);
                services.AddSingleton<ILlmClient>(new ThrowingLlmClient());
                services.AddSingleton<TimeProvider>(_clock);
            },
            settings: new Dictionary<string, string?> { ["Demo:Enabled"] = enabled.ToString() });

    private sealed class NotYetSource : IAttendanceSource
    {
        public Task<SourceResult> FetchAsync(
            Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster, CancellationToken cancellationToken) =>
            Task.FromResult<SourceResult>(new SourceResult.NotYet());
    }
}
