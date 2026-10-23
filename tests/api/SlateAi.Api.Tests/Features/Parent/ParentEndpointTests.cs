using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Features.Audio;
using SlateAi.Api.Tests.Features.CatchUps;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Parent;

public sealed class ParentEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DateOnly Friday = new(2026, 10, 23);
    private static readonly Guid Ravi = Guid.NewGuid();
    private const string Token = "AbCdEfGhIjKlMnOpQrStUv";
    private const string EveningToken = "ZyXwVuTsRqPoNmLkJiHgFe";

    private readonly MovableClock _clock = new() { Now = SchoolClock.At(Friday, new TimeOnly(19, 0)) };
    private readonly FakeParentStore _parents = new();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();
    private readonly FakeAudioStore _audio = new();
    private readonly FakeEveningStore _evening = new(ClassId, Guid.NewGuid());
    private readonly Guid _assignmentId = Guid.NewGuid();
    private readonly Guid _script;
    private readonly Guid _disclosure;
    private readonly Guid _otherScript;

    public ParentEndpointTests()
    {
        var package = Guid.NewGuid();
        var silence = FakeTextToSpeech.Silence(2);
        _script = _audio.ReplaceScriptAsync(package, "te", silence, Ct).GetAwaiter().GetResult().Id;
        _disclosure = _audio.SaveSharedAsync(AudioKind.Disclosure, "te", "hash", silence, Ct).GetAwaiter().GetResult().Id;
        _otherScript = _audio.ReplaceScriptAsync(Guid.NewGuid(), "te", silence, Ct).GetAwaiter().GetResult().Id;
        _parents.CatchUps[Token] = Record();
        _parents.EveningLinks[EveningToken] = new EveningLink(Ravi, ClassId, Friday, _clock.Now.AddDays(14), false, null);
        _evening.Children.Add(new EveningChild(Ravi, 4, "Ravi", "te", "yes", false));
        _evening.Templates.Add(new TemplateRecord(TemplateId, "te", "2", "ఈ రోజు మేము {child}ని గుర్తు చేసుకున్నాం.", "", "", "approved", _clock.Now));
        _evening.Messages.Add((Friday, new NewEveningMessage(
            Ravi, TemplateId, Guid.NewGuid(), "te", new SlotValues("", ""), EveningToken, _clock.Now, _clock.Now.AddDays(14))));
    }

    [Fact]
    public async Task Approved_catch_up_serves_the_lesson_without_the_correct_answer()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/api/p/{Token}", Ct);
        var raw = await response.Content.ReadAsStringAsync(Ct);
        var page = (await response.Content.ReadFromJsonAsync<ParentCatchUpResponse>(Ct))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"kind\":\"catchUp\"", raw);
        Assert.DoesNotContain("correct", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answer\":", raw); // the activity's and the check's answers stay on the server
        Assert.Equal("Ravi", page.ChildName);
        Assert.Equal("Shobha", page.TeacherName);
        Assert.Equal("te", page.Language);
        Assert.False(page.Stopped);
        Assert.False(page.Answered);
        Assert.Equal(new TextbookPage("textbook", 2, 15), page.Homework);
        Assert.Equal([0, 1, 2], page.Check!.Options.Select(o => o.Id));
        Assert.Equal([35, 25, 15], page.Check.Options.Select(o => o.Number));
        Assert.Equal(43, page.Activity!.Number);
        Assert.Equal("ఈ రోజు పాఠం …", page.Transcript!.Script);
        Assert.Equal("ఇది మీ బిడ్డ టీచర్ పంపిన పాఠం.", page.Transcript.DisclosureText);
        Assert.Equal(
            [$"/api/p/{Token}/audio/{_disclosure}", $"/api/p/{Token}/audio/{_script}"],
            page.Audio!.Segments.Select(s => s.Url));
    }

    [Theory]
    [InlineData(PackageStatus.Ready, 2)]
    [InlineData(PackageStatus.Generating, 2)]
    [InlineData(PackageStatus.Approved, 1)] // approved an older version
    public async Task Anything_not_approved_at_its_version_is_404(string status, int approvedVersion)
    {
        _parents.CatchUps[Token] = Record() with { Status = status, ApprovedScriptVersion = approvedVersion };
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/p/{Token}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/p/{Token}/audio/{_script}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Stopped_family_gets_no_lesson_only_the_stopped_view()
    {
        _parents.Stopped[Ravi] = _clock.Now;
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var page = (await client.GetFromJsonAsync<ParentCatchUpResponse>($"/api/p/{Token}", Ct))!;

        Assert.True(page.Stopped);
        Assert.Equal("Ravi", page.ChildName);
        Assert.Null(page.Audio);
        Assert.Null(page.Transcript);
        Assert.Null(page.Activity);
        Assert.Null(page.Check);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/p/{Token}/audio/{_script}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Expired_link_is_410_with_the_homework_page()
    {
        _clock.Now = _clock.Now.AddDays(15);
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/api/p/{Token}", Ct);
        var expired = (await response.Content.ReadFromJsonAsync<ParentExpiredResponse>(Ct))!;

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Equal("Shobha", expired.TeacherName);
        Assert.Equal("Ravi", expired.ChildName);
        Assert.Equal(new TextbookPage("textbook", 2, 15), expired.Homework);
    }

    [Fact]
    public async Task Unknown_token_is_404()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/p/AAAAAAAAAAAAAAAAAAAAAA", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/p/short", Ct)).StatusCode);
    }

    [Fact]
    public async Task Audio_plays_only_this_catch_ups_segments()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var script = await client.GetAsync($"/api/p/{Token}/audio/{_script}", Ct);
        var disclosure = await client.GetAsync($"/api/p/{Token}/audio/{_disclosure}", Ct);

        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
        Assert.Equal("audio/ogg", script.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.OK, disclosure.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/p/{Token}/audio/{_otherScript}", Ct)).StatusCode);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(2, false)]
    public async Task Answer_is_scored_by_code_and_the_response_never_says_which(int choice, bool correct)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync($"/api/p/{Token}/answer", new AnswerRequest(choice, Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"answered\":true}", await response.Content.ReadAsStringAsync(Ct));
        var stored = Assert.Single(_parents.Answers);
        Assert.Equal(correct, stored.Correct);
        Assert.Equal(correct ? CheckOutcome.CaughtUp : CheckOutcome.NeedsBuddy, stored.Outcome);
        Assert.Equal(_assignmentId, stored.AssignmentId);
    }

    [Fact]
    public async Task First_answer_counts_and_replays_change_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var request = new AnswerRequest(0, Guid.NewGuid());

        var first = await client.PostAsJsonAsync($"/api/p/{Token}/answer", request, Ct);
        var replay = await client.PostAsJsonAsync($"/api/p/{Token}/answer", request, Ct);
        var second = await client.PostAsJsonAsync($"/api/p/{Token}/answer", new AnswerRequest(1, Guid.NewGuid()), Ct);
        var page = (await client.GetFromJsonAsync<ParentCatchUpResponse>($"/api/p/{Token}", Ct))!;

        Assert.All([first, replay, second], r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(CheckOutcome.NeedsBuddy, Assert.Single(_parents.Answers).Outcome);
        Assert.True(page.Answered);
    }

    [Fact]
    public async Task Answer_is_refused_when_stopped_expired_or_not_approved()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        Task<HttpResponseMessage> Answer() =>
            client.PostAsJsonAsync($"/api/p/{Token}/answer", new AnswerRequest(1, Guid.NewGuid()), Ct);

        _parents.Stopped[Ravi] = _clock.Now;
        var stopped = await Answer();
        _parents.Stopped.Clear();
        _clock.Now = _clock.Now.AddDays(15);
        var expired = await Answer();
        _clock.Now = _clock.Now.AddDays(-15);
        _parents.CatchUps[Token] = Record() with { Status = PackageStatus.Ready };
        var unapproved = await Answer();

        Assert.Equal(HttpStatusCode.Conflict, stopped.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, expired.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, unapproved.StatusCode);
        Assert.Empty(_parents.Answers);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task Answer_outside_the_pictures_is_400(int choice)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync($"/api/p/{Token}/answer", new AnswerRequest(choice, Guid.NewGuid()), Ct);
        var missingId = await client.PostAsJsonAsync($"/api/p/{Token}/answer", new AnswerRequest(1, Guid.Empty), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingId.StatusCode);
        Assert.Empty(_parents.Answers);
    }

    [Fact]
    public async Task Stop_and_restart_are_idempotent_and_keep_the_first_stop_time()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var stop = await client.PostAsJsonAsync($"/api/p/{Token}/stop", new StopRequest(Guid.NewGuid()), Ct);
        var stoppedAt = _parents.Stopped[Ravi];
        _clock.Now = _clock.Now.AddMinutes(5);
        var again = await client.PostAsJsonAsync($"/api/p/{Token}/stop", new StopRequest(Guid.NewGuid()), Ct);
        var page = (await client.GetFromJsonAsync<ParentCatchUpResponse>($"/api/p/{Token}", Ct))!;

        Assert.Equal(HttpStatusCode.OK, stop.StatusCode);
        Assert.True((await stop.Content.ReadFromJsonAsync<StopResponse>(Ct))!.Stopped);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(stoppedAt, _parents.Stopped[Ravi]);
        Assert.True(page.Stopped);

        var restart = await client.PostAsJsonAsync($"/api/p/{Token}/restart", new StopRequest(Guid.NewGuid()), Ct);
        var restartAgain = await client.PostAsJsonAsync($"/api/p/{Token}/restart", new StopRequest(Guid.NewGuid()), Ct);

        Assert.False((await restart.Content.ReadFromJsonAsync<StopResponse>(Ct))!.Stopped);
        Assert.Equal(HttpStatusCode.OK, restartAgain.StatusCode);
        Assert.Empty(_parents.Stopped);
    }

    [Fact]
    public async Task Stop_works_from_an_evening_link_and_after_expiry()
    {
        _clock.Now = _clock.Now.AddDays(30);
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var fromEvening = await client.PostAsJsonAsync($"/api/p/{EveningToken}/stop", new StopRequest(Guid.NewGuid()), Ct);
        _parents.Stopped.Clear();
        var expiredCatchUp = await client.PostAsJsonAsync($"/api/p/{Token}/stop", new StopRequest(Guid.NewGuid()), Ct);
        var unknown = await client.PostAsJsonAsync("/api/p/AAAAAAAAAAAAAAAAAAAAAA/stop", new StopRequest(Guid.NewGuid()), Ct);
        var noRequestId = await client.PostAsJsonAsync($"/api/p/{Token}/stop", new StopRequest(Guid.Empty), Ct);

        Assert.Equal(HttpStatusCode.OK, fromEvening.StatusCode);
        Assert.Equal(HttpStatusCode.OK, expiredCatchUp.StatusCode);
        Assert.True(_parents.Stopped.ContainsKey(Ravi));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noRequestId.StatusCode);
    }

    [Theory]
    [InlineData(ReasonCategory.Unwell)]
    [InlineData(ReasonCategory.TravelOrWork)]
    [InlineData(ReasonCategory.Festival)]
    [InlineData(ReasonCategory.NeededAtHome)]
    [InlineData(ReasonCategory.Other)]
    public async Task Reason_is_stored_as_a_category_for_that_child_and_day(string category)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync($"/api/p/{EveningToken}/reason", new ReasonRequest(category, Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"{{\"category\":\"{category}\"}}", await response.Content.ReadAsStringAsync(Ct));
        var stored = Assert.Single(_parents.Reasons);
        Assert.Equal((Ravi, ClassId, Friday, category), (stored.ChildId, stored.ClassId, stored.Date, stored.Category));
    }

    [Theory]
    [InlineData("fever")]
    [InlineData("Unwell")]
    [InlineData("")]
    public async Task Reason_outside_the_five_pictures_is_400_and_nothing_is_stored(string category)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync($"/api/p/{EveningToken}/reason", new ReasonRequest(category, Guid.NewGuid()), Ct);
        var noRequestId = await client.PostAsJsonAsync(
            $"/api/p/{EveningToken}/reason", new ReasonRequest(ReasonCategory.Other, Guid.Empty), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noRequestId.StatusCode);
        Assert.Empty(_parents.Reasons);
    }

    [Fact]
    public async Task Reason_belongs_to_an_evening_link_not_a_catch_up()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var catchUp = await client.PostAsJsonAsync($"/api/p/{Token}/reason", new ReasonRequest(ReasonCategory.Other, Guid.NewGuid()), Ct);
        var unknown = await client.PostAsJsonAsync(
            "/api/p/AAAAAAAAAAAAAAAAAAAAAA/reason", new ReasonRequest(ReasonCategory.Other, Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.NotFound, catchUp.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Empty(_parents.Reasons);
    }

    [Fact]
    public async Task Reason_is_refused_once_expired_or_stopped()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        Task<HttpResponseMessage> Send() =>
            client.PostAsJsonAsync($"/api/p/{EveningToken}/reason", new ReasonRequest(ReasonCategory.Unwell, Guid.NewGuid()), Ct);

        _parents.Stopped[Ravi] = _clock.Now;
        var stopped = await Send();
        _parents.Stopped.Clear();
        _clock.Now = _clock.Now.AddDays(15);
        var expired = await Send();

        Assert.Equal(HttpStatusCode.Conflict, stopped.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, expired.StatusCode);
        Assert.Empty(_parents.Reasons);
    }

    [Fact]
    public async Task A_later_tap_replaces_the_reason_and_a_replay_changes_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var first = new ReasonRequest(ReasonCategory.Unwell, Guid.NewGuid());

        await client.PostAsJsonAsync($"/api/p/{EveningToken}/reason", first, Ct);
        await client.PostAsJsonAsync($"/api/p/{EveningToken}/reason", new ReasonRequest(ReasonCategory.Festival, Guid.NewGuid()), Ct);
        var replay = await client.PostAsJsonAsync($"/api/p/{EveningToken}/reason", first, Ct);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(ReasonCategory.Festival, Assert.Single(_parents.Reasons).Category);
    }

    [Fact]
    public async Task Evening_link_shows_the_reason_already_given()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var before = (await client.GetFromJsonAsync<ParentEveningResponse>($"/api/p/{EveningToken}", Ct))!;
        await client.PostAsJsonAsync($"/api/p/{EveningToken}/reason", new ReasonRequest(ReasonCategory.NeededAtHome, Guid.NewGuid()), Ct);
        var raw = await client.GetStringAsync($"/api/p/{EveningToken}", Ct);
        var after = (await client.GetFromJsonAsync<ParentEveningResponse>($"/api/p/{EveningToken}", Ct))!;

        Assert.Null(before.Reason);
        Assert.Equal(ReasonCategory.NeededAtHome, after.Reason);
        Assert.Contains("\"kind\":\"evening\"", raw);
        Assert.Equal("ఈ రోజు మేము Raviని గుర్తు చేసుకున్నాం.", after.Text);
    }

    private ParentCatchUpRecord Record() => new(
        _assignmentId,
        Ravi,
        "Ravi",
        "Shobha",
        "te",
        PackageStatus.Sent,
        ScriptVersion: 2,
        ApprovedScriptVersion: 2,
        ["M3.SUB.BORROW"],
        "ఈ రోజు పాఠం …",
        ["ಕಳೆ", "ದಶಕ"],
        new ActivityData("M3.SUB.BORROW", "sub", "43 − 18", [43, 18], 25, "ఒక కట్ట విప్పు.", "సీసా మూతలు వాడండి."),
        new CheckData("M3.SUB.BORROW", "sub", "52 − 27", [35, 25, 15], CorrectIndex: 1, Answer: 25, "సరైన బొమ్మను నొక్కు"),
        new AudioSegments(
            [
                new AudioSegment(AudioKind.Disclosure, AudioSegments.Url(_disclosure), 4),
                new AudioSegment(AudioKind.Script, AudioSegments.Url(_script), 50),
            ],
            54, 150_000, "ఇది మీ బిడ్డ టీచర్ పంపిన పాఠం."),
        ExpiresAt: _clock.Now.AddDays(14),
        Stopped: false,
        Answered: false);

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<IParentStore>(_parents);
            services.AddSingleton<IEveningStore>(_evening);
            services.AddSingleton<ILlmClient>(new ThrowingLlmClient()); // no AI anywhere on the family's side
            services.AddSingleton<IAudioStore>(_audio);
            services.AddSingleton(A3ValidatorTests.Book);
            services.AddSingleton<TimeProvider>(_clock);
        });
}
