using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.LessonAudio;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Tests.Features.Attendance;
using SlateAi.Api.Tests.Features.Audio;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Features.Parent;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.LessonAudio;

/// <summary>
/// T-62 on the API: the optional lesson audio is only ever sent after she ticks the box, listens and approves the exact
/// clip (guardrail 3); the coded evening message is the same with or without it.
/// </summary>
public sealed class LessonAudioEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 20);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly string Url = $"/api/classes/{ClassId}/lesson-audio/{Today:yyyy-MM-dd}";
    private static readonly string SendUrl = $"/api/classes/{ClassId}/evening-messages/send";

    private const string Telugu =
        "{\"script\":\"ఈ రోజు మేము ముఖ విలువ నేర్చుకున్నాం. 89 లో 8 ముఖ విలువ, 80 స్థాన విలువ. పుస్తకం 15 వ పేజీలో హోంవర్క్ చేసి మీ టీచర్‌కు చూపించండి.\"}";

    private const string Kannada =
        "{\"script\":\"ಇಂದು ನಾವು ಮುಖಬೆಲೆ ಕಲಿತೆವು. 89 ರಲ್ಲಿ 8 ಮುಖಬೆಲೆ, 80 ಸ್ಥಾನಬೆಲೆ. ಪುಸ್ತಕದ 15 ನೇ ಪುಟದ ಮನೆಕೆಲಸ ಮಾಡಿ ನಿಮ್ಮ ಟೀಚರ್‌ಗೆ ತೋರಿಸಿ.\"}";

    private static readonly EveningWording Wording = new("1", new Dictionary<string, LanguageWording>
    {
        ["kn"] = new("ಇಂದು {child} … {lesson} … {page}", "ಭಾಗ {part}, ಪುಟ {page}", "ಪುಟ {page}"),
        ["te"] = new("ఈ రోజు {child} … {lesson} … {page}", "భాగం {part}, పేజీ {page}", "పేజీ {page}"),
    });

    private static readonly CurriculumGraph Book = new([LessonScriptTests.Lesson], []);
    private static readonly EveningChild Ravi = new(Guid.NewGuid(), 4, "Ravi", "te", "yes", false);
    private static readonly EveningChild Asha = new(Guid.NewGuid(), 1, "Asha", "kn", "yes", false);
    private static readonly EveningChild[] All = [Ravi, Asha];

    private readonly FakeEveningStore _evening = new(ClassId, TeacherId) { TeacherName = "Shobha" };
    private readonly FakeAttendanceStore _attendance = new(ClassId, All.Select(c => c.Id).ToList());
    private readonly FakeLessonAudioStore _lessons = new();
    private readonly FakeAudioStore _audio = new();
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly GatedLlm _llm;
    private readonly MovableClock _clock = new() { Now = SchoolClock.At(Today, new TimeOnly(18, 30)) };

    public LessonAudioEndpointTests()
    {
        _llm = new GatedLlm(_aiCalls);
        _evening.Children.AddRange(All);
        _evening.ApprovedLessonAudio = _lessons.ApprovedFor;
        _attendance.SetDay(Today, All.Select(c => new AttendanceRow(c.Id, false, AttendanceSources.Manual, null, null, null)).ToList());
        _evening.GetTemplatesAsync(ClassId, Wording, Ct).GetAwaiter().GetResult();
        _evening.ApproveAsync(ClassId, Guid.NewGuid(), Wording.Version, TeacherId, Ct).GetAwaiter().GetResult();
        _evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today, ["M3.PV.FACE"]));
    }

    [Fact]
    public async Task Unticked_by_default_and_the_card_is_offered_once_the_lesson_and_template_are_in()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var day = (await client.GetFromJsonAsync<LessonAudioDayResponse>(Url, Ct))!;

        Assert.True(day.Available);
        Assert.False(day.Enabled);
        Assert.Empty(day.Items);
    }

    [Fact]
    public async Task Ticking_starts_one_clip_per_language_of_tonights_families()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await Tick(client);
        var day = (await response.Content.ReadFromJsonAsync<LessonAudioDayResponse>(Ct))!;

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.True(day.Enabled);
        Assert.Equal(["kn", "te"], day.Items.Select(i => i.Language));
        Assert.All(day.Items, i => Assert.Equal(1, i.Children));
        Assert.All(day.Items, i => Assert.Equal(LessonAudioStatus.Generating, i.Status));
        Assert.DoesNotContain(_aiCalls.Calls, c => c.Purpose != "a8-lesson-recap"); // only A8, never the weekly catch-up prompts
    }

    [Fact]
    public async Task A_clip_becomes_ready_with_disclosure_then_lesson_and_the_words_she_will_approve()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await Tick(client);

        var day = await ReadyAsync(client);

        var te = day.Items.Single(i => i.Language == "te");
        Assert.Equal([AudioKind.Disclosure, AudioKind.Script], te.Segments.Select(s => s.Kind));
        Assert.StartsWith("ఈ రోజు మేము", te.Transcript);
        Assert.NotNull(te.ClipId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(te.Segments[1].Url, Ct)).StatusCode); // she can play it
    }

    [Fact]
    public async Task Approving_the_clip_she_listened_to_works_and_replays_are_harmless()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await Tick(client);
        var te = (await ReadyAsync(client)).Items.Single(i => i.Language == "te");
        var requestId = Guid.NewGuid();

        var first = await Approve(client, "te", te.ClipId!.Value, requestId);
        var replay = await Approve(client, "te", te.ClipId.Value, requestId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var day = (await first.Content.ReadFromJsonAsync<LessonAudioDayResponse>(Ct))!;
        Assert.Equal(LessonAudioStatus.Approved, day.Items.Single(i => i.Language == "te").Status);
        Assert.Equal(LessonAudioStatus.Ready, day.Items.Single(i => i.Language == "kn").Status); // kn is still waiting for her
    }

    [Fact]
    public async Task Approving_a_clip_that_is_not_ready_or_not_the_one_she_was_shown_is_409()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await Tick(client);

        var tooEarly = await Approve(client, "te", Guid.NewGuid());
        var ready = await ReadyAsync(client);
        var wrongClip = await Approve(client, "te", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, tooEarly.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, wrongClip.StatusCode);
        Assert.Empty(_lessons.ApprovedFor(Today));
        Assert.All(ready.Items, i => Assert.Equal(LessonAudioStatus.Ready, i.Status));
    }

    [Fact]
    public async Task Try_again_gives_a_new_clip_and_the_old_approval_does_not_carry_over()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await Tick(client);
        var old = (await ReadyAsync(client)).Items.Single(i => i.Language == "te").ClipId!.Value;
        await Approve(client, "te", old);

        var response = await client.PostAsJsonAsync($"{Url}/te/regenerate", new RegenerateLessonAudioRequest(Guid.NewGuid()), Ct);
        var day = await ReadyAsync(client);
        var stale = await Approve(client, "te", old);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var te = day.Items.Single(i => i.Language == "te");
        Assert.Equal(LessonAudioStatus.Ready, te.Status); // approval cleared: she must listen and approve again
        Assert.NotEqual(old, te.ClipId);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.DoesNotContain("te", _lessons.ApprovedFor(Today).Keys);
    }

    [Fact]
    public async Task Unticking_removes_the_clips_and_approvals()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await Tick(client);
        var te = (await ReadyAsync(client)).Items.Single(i => i.Language == "te");
        await Approve(client, "te", te.ClipId!.Value);

        var response = await Tick(client, enabled: false);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(_lessons.Rows);
        Assert.Empty(_lessons.ApprovedFor(Today));
    }

    [Theory]
    [InlineData("noLesson")]
    [InlineData("noTemplate")]
    public async Task Ticking_is_409_and_creates_nothing_until_the_lesson_is_confirmed_and_the_template_approved(string missing)
    {
        if (missing == "noLesson")
        {
            _evening.Lessons.Clear();
        }
        else
        {
            _evening.Templates.Clear();
        }

        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await Tick(client);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<LessonAudioDayResponse>(Ct))!.Available);
        Assert.Empty(_lessons.Rows);
    }

    [Fact]
    public async Task Only_an_approved_clip_goes_with_the_message_and_only_for_its_language()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await Tick(client);
        var ready = await ReadyAsync(client);
        var te = ready.Items.Single(i => i.Language == "te");
        // kn is ready but never approved.
        await Approve(client, "te", te.ClipId!.Value);

        var sent = await client.PostAsJsonAsync(SendUrl, new SendEveningRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var withAudio = _evening.Messages.ToDictionary(m => m.Message.ChildId, m => m.Message.LessonAudioId);
        Assert.NotNull(withAudio[Ravi.Id]);
        Assert.Null(withAudio[Asha.Id]); // ready is not approved: the plain message
    }

    [Fact]
    public async Task Unticked_the_message_goes_out_plain_and_without_calling_an_llm()
    {
        await using var factory = FakeApiWith(new ThrowingLlmClient());
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var sent = await client.PostAsJsonAsync(SendUrl, new SendEveningRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.Equal(2, _evening.Messages.Count);
        Assert.All(_evening.Messages, m => Assert.Null(m.Message.LessonAudioId));
    }

    [Fact]
    public async Task Once_the_messages_have_gone_it_is_read_only()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await Tick(client);
        var te = (await ReadyAsync(client)).Items.Single(i => i.Language == "te");
        await client.PostAsJsonAsync(SendUrl, new SendEveningRequest(Guid.NewGuid()), Ct);

        var untick = await Tick(client, enabled: false);
        var late = await Approve(client, "te", te.ClipId!.Value);

        Assert.Equal(HttpStatusCode.Conflict, untick.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Empty(_lessons.ApprovedFor(Today));
        Assert.Contains(_evening.Messages, m => m.Message.LessonAudioId is null); // already-sent messages stay plain
        var day = (await client.GetFromJsonAsync<LessonAudioDayResponse>(Url, Ct))!;
        Assert.True(day.Sent);
        Assert.False(day.Available);
    }

    [Fact]
    public async Task Another_class_gets_404_and_signed_out_gets_401_and_a_missing_request_id_is_400()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync(Url, new SetLessonAudioRequest(Guid.NewGuid(), true), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(Url, new SetLessonAudioRequest(Guid.Empty, true), Ct)).StatusCode);
    }

    private Task<HttpResponseMessage> Tick(HttpClient client, bool enabled = true) =>
        client.PutAsJsonAsync(Url, new SetLessonAudioRequest(Guid.NewGuid(), enabled), Ct);

    private Task<HttpResponseMessage> Approve(HttpClient client, string language, Guid clipId, Guid? requestId = null) =>
        client.PostAsJsonAsync($"{Url}/{language}/approve", new ApproveLessonAudioRequest(requestId ?? Guid.NewGuid(), clipId), Ct);

    /// <summary>Lets the clips be made and waits until none is generating.</summary>
    private async Task<LessonAudioDayResponse> ReadyAsync(HttpClient client)
    {
        _llm.Open();
        for (var i = 0; i < 100; i++)
        {
            var day = (await client.GetFromJsonAsync<LessonAudioDayResponse>(Url, Ct))!;
            if (day.Items.Count > 0 && day.Items.All(item => item.Status != LessonAudioStatus.Generating))
            {
                return day;
            }

            await Task.Delay(50, Ct);
        }

        throw new TimeoutException(
            "The lesson clips were not made: " + string.Join(", ", _lessons.Rows.Select(r => $"{r.Language} {r.Status} {r.Verdict}")));
    }

    private ApiFactory Create() => FakeApiWith(_llm);

    private ApiFactory FakeApiWith(ILlmClient llm) =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<IEveningStore>(_evening);
            services.AddSingleton<IParentStore>(new FakeParentStore());
            services.AddSingleton<IAttendanceStore>(_attendance);
            services.AddSingleton<IAttendanceSource>(new NotYetSource());
            services.AddSingleton<ILessonAudioStore>(_lessons);
            services.AddSingleton<IAudioStore>(_audio);
            services.AddSingleton<IAiCallStore>(_aiCalls);
            services.AddSingleton(Book);
            services.AddSingleton(Wording);
            services.AddSingleton(llm);
            services.AddSingleton<TimeProvider>(_clock);
        });

    /// <summary>Answers A8 with a valid script for the request's language, but only once the test lets it (so "generating" can be seen).</summary>
    private sealed class GatedLlm(FakeAiCallStore calls) : ILlmClient
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Open() => _gate.TrySetResult();

        public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            await _gate.Task.WaitAsync(cancellationToken);
            var json = request.UserContent.Contains("\"language\":\"kn\"") ? Kannada : Telugu;
            var call = new AiCall { Id = Guid.NewGuid(), Purpose = request.Prompt.Id, Model = "stub", CreatedAt = DateTimeOffset.UnixEpoch };
            await calls.AddAsync(call, cancellationToken);
            return new LlmResponse(json, "stub", new LlmUsage(0, 0, 0), call.Id);
        }
    }

    private sealed class NotYetSource : IAttendanceSource
    {
        public Task<SourceResult> FetchAsync(
            Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster, CancellationToken cancellationToken) =>
            Task.FromResult<SourceResult>(new SourceResult.NotYet());
    }
}
