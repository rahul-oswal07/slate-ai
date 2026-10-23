using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Tests.Features.Attendance;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Tests.Features.Parent;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.EveningMessages;

public sealed class EveningEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 20);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly string DayUrl = $"/api/classes/{ClassId}/evening-messages/{Today:yyyy-MM-dd}";
    private static readonly string SendUrl = $"/api/classes/{ClassId}/evening-messages/send";
    private static readonly string TemplateUrl = $"/api/classes/{ClassId}/message-template";

    private static readonly EveningWording Wording = new("1", new Dictionary<string, LanguageWording>
    {
        ["kn"] = new("ಇಂದು {child} … {lesson} … {page}", "ಭಾಗ {part}, ಪುಟ {page}", "ಪುಟ {page}"),
        ["te"] = new("ఈ రోజు {child} … {lesson} … {page}", "భాగం {part}, పేజీ {page}", "పేజీ {page}"),
        ["ta"] = new("இன்று {child} … {lesson} … {page}", "பகுதி {part}, பக்கம் {page}", "பக்கம் {page}"),
    });

    // No Tamil name: a Tamil family's message is refused, never half-filled.
    private static readonly CurriculumGraph Book = new(
        [
            new Competency("M3.SUB.BORROW", "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ", "Subtraction with borrowing", [], null, 5, 4, [],
                new TextbookPage("textbook", 2, 15), NameTe: "పది తీసుకుని తీసివేత"),
        ],
        []);

    private static readonly EveningChild Ravi = new(Guid.NewGuid(), 4, "Ravi", "te", "yes", false);
    private static readonly EveningChild Asha = new(Guid.NewGuid(), 1, "Asha", "kn", "yes", false);
    private static readonly EveningChild NoForm = new(Guid.NewGuid(), 2, "Bhavana", "kn", "no", false);
    private static readonly EveningChild NoPhone = new(Guid.NewGuid(), 5, "Darshan", "kn", "nophone", false);
    private static readonly EveningChild Stopped = new(Guid.NewGuid(), 6, "Esha", "kn", "yes", true);
    private static readonly EveningChild Tamil = new(Guid.NewGuid(), 7, "Kavin", "ta", "yes", false);
    private static readonly EveningChild Present = new(Guid.NewGuid(), 8, "Latha", "kn", "yes", false);

    private static readonly EveningChild[] All = [Ravi, Asha, NoForm, NoPhone, Stopped, Tamil, Present];

    private readonly FakeEveningStore _store = new(ClassId, TeacherId) { TeacherName = "Shobha" };
    private readonly FakeAttendanceStore _attendance = new(ClassId, All.Select(c => c.Id).ToList());
    private readonly MovableClock _clock = new() { Now = Ist(18, 30) };

    public EveningEndpointTests()
    {
        _store.Children.AddRange(All);
        _attendance.SetDay(Today, All
            .Select(c => new AttendanceRow(c.Id, c == Present, AttendanceSources.Manual, null, null, null))
            .ToList());
    }

    [Fact]
    public async Task Sends_only_to_absent_families_with_consent_who_have_not_stopped()
    {
        ApproveTemplate();
        ConfirmLesson();

        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var day = (await response.Content.ReadFromJsonAsync<EveningDayResponse>(Ct))!;
        Assert.Equal(6, day.Absent);
        Assert.Equal(2, day.Families);
        Assert.Equal(2, day.Sent);
        Assert.Equal(
            new Dictionary<Guid, string>
            {
                [Ravi.Id] = EveningOutcome.Sent,
                [Asha.Id] = EveningOutcome.Sent,
                [NoForm.Id] = EveningOutcome.NoConsent,
                [NoPhone.Id] = EveningOutcome.NoPhone,
                [Stopped.Id] = EveningOutcome.Stopped,
                [Tamil.Id] = EveningOutcome.NoWording,
            },
            day.Children.ToDictionary(c => c.ChildId, c => c.Outcome));
        Assert.Equal(new HashSet<Guid> { Asha.Id, Ravi.Id }, _store.Messages.Select(m => m.Message.ChildId).ToHashSet());
    }

    [Fact]
    public async Task Unapproved_template_is_409_and_sends_nothing()
    {
        ConfirmLesson();

        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(EveningBlock.TemplateNotApproved,
            (await response.Content.ReadFromJsonAsync<EveningDayResponse>(Ct))!.Blocked);
        Assert.Empty(_store.Messages);
    }

    [Fact]
    public async Task Unconfirmed_lesson_is_409_and_sends_nothing()
    {
        ApproveTemplate();
        _store.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today.AddDays(-1), ["M3.SUB.BORROW"]));

        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(EveningBlock.LessonNotConfirmed,
            (await response.Content.ReadFromJsonAsync<EveningDayResponse>(Ct))!.Blocked);
        Assert.Empty(_store.Messages);
    }

    [Fact]
    public async Task No_attendance_is_409()
    {
        ApproveTemplate();
        ConfirmLesson();
        _attendance.SetDay(Today, []);

        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(_store.Messages);
    }

    [Fact]
    public async Task Before_6_pm_is_409_unless_the_demo_is_on()
    {
        ApproveTemplate();
        ConfirmLesson();
        _clock.Now = Ist(17, 59);

        var school = await SendAsync(demo: false);
        Assert.Equal(HttpStatusCode.Conflict, school.StatusCode);
        Assert.Empty(_store.Messages);

        var demo = await SendAsync(demo: true);
        Assert.Equal(HttpStatusCode.OK, demo.StatusCode);
        Assert.Equal(2, _store.Messages.Count);
    }

    [Fact]
    public async Task Sending_again_sends_nothing_new()
    {
        ApproveTemplate();
        ConfirmLesson();

        await SendAsync();
        var again = await SendAsync();

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(2, _store.Messages.Count);
    }

    [Fact]
    public async Task No_llm_is_called_on_this_path()
    {
        ApproveTemplate();
        ConfirmLesson();

        // The factory registers an ILlmClient that throws; a send that used it would fail.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync()).StatusCode);
    }

    [Fact]
    public async Task Tonight_card_counts_before_the_send()
    {
        ApproveTemplate();
        ConfirmLesson();
        _clock.Now = Ist(15, 0);

        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var day = (await client.GetFromJsonAsync<EveningDayResponse>(DayUrl, Ct))!;

        Assert.Null(day.Blocked);
        Assert.Equal(6, day.Absent);
        Assert.Equal(2, day.Families);
        Assert.Equal(0, day.Sent);
        Assert.Equal(new TimeOnly(18, 0), day.SendsAt);
        Assert.Empty(_store.Messages);
    }

    [Fact]
    public async Task Template_is_shown_filled_with_a_telugu_example()
    {
        ConfirmLesson();

        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var template = (await client.GetFromJsonAsync<MessageTemplateResponse>(TemplateUrl, Ct))!;

        Assert.Equal("draft", template.Status);
        Assert.Equal("1", template.Version);
        var telugu = template.Languages.Single(l => l.Language == "te").Example;
        Assert.Equal("ఈ రోజు Ravi … పది తీసుకుని తీసివేత … భాగం 2, పేజీ 15", string.Concat(telugu.Select(p => p.Text)));
        Assert.Equal(["child", "lesson", "page"], telugu.Where(p => p.Slot is not null).Select(p => p.Slot!));
    }

    [Fact]
    public async Task Approving_is_idempotent_and_a_stale_version_is_409_with_the_latest()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        var requestId = Guid.NewGuid();

        var first = await client.PostAsJsonAsync($"{TemplateUrl}/approve", new ApproveTemplateRequest(requestId, "1"), Ct);
        var replay = await client.PostAsJsonAsync($"{TemplateUrl}/approve", new ApproveTemplateRequest(requestId, "1"), Ct);
        var stale = await client.PostAsJsonAsync($"{TemplateUrl}/approve", new ApproveTemplateRequest(Guid.NewGuid(), "0"), Ct);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("approved", (await first.Content.ReadFromJsonAsync<MessageTemplateResponse>(Ct))!.Status);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(1, _store.Approvals);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("1", (await stale.Content.ReadFromJsonAsync<MessageTemplateResponse>(Ct))!.Version);
    }

    [Fact]
    public async Task Parent_link_shows_the_message_in_the_familys_language()
    {
        ApproveTemplate();
        ConfirmLesson();
        await SendAsync();
        var token = _store.Messages.Single(m => m.Message.ChildId == Ravi.Id).Message.Token;

        await using var factory = Create();
        using var anonymous = factory.CreateHttpsClient();
        var message = (await anonymous.GetFromJsonAsync<ParentEveningResponse>($"/api/p/{token}", Ct))!;

        Assert.Equal("te", message.Language);
        Assert.Equal("Ravi", message.ChildName);
        Assert.Equal("Shobha", message.TeacherName);
        Assert.Equal("ఈ రోజు Ravi … పది తీసుకుని తీసివేత … భాగం 2, పేజీ 15", message.Text);
        Assert.False(message.Stopped);
        Assert.Equal(22, token.Length);
    }

    [Fact]
    public async Task Parent_link_is_404_when_unknown_and_410_once_expired()
    {
        ApproveTemplate();
        ConfirmLesson();
        await SendAsync();
        var token = _store.Messages[0].Message.Token;
        _clock.Now = Ist(18, 30).AddDays(14);

        await using var factory = Create();
        using var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/p/AAAAAAAAAAAAAAAAAAAAAA", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await anonymous.GetAsync($"/api/p/{token}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Signed_out_is_401_and_another_class_is_404()
    {
        await using var factory = Create();
        using var anonymous = factory.CreateHttpsClient();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(DayUrl, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync(SendUrl, new SendEveningRequest(Guid.NewGuid()), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(TemplateUrl, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.PostAsJsonAsync(SendUrl, new SendEveningRequest(Guid.NewGuid()), Ct)).StatusCode);
    }

    [Fact]
    public async Task Worker_does_nothing_before_6_pm_and_sends_after()
    {
        ApproveTemplate();
        ConfirmLesson();
        await using var factory = Create();
        var worker = new EveningSendWorker(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EveningOptions()),
            _clock,
            NullLogger<EveningSendWorker>.Instance);

        _clock.Now = Ist(17, 59);
        Assert.Equal(0, await worker.RunOnceAsync(Ct));
        Assert.Empty(_store.Messages);

        _clock.Now = Ist(18, 0);
        Assert.Equal(1, await worker.RunOnceAsync(Ct));
        Assert.Equal(2, _store.Messages.Count);

        _clock.Now = Ist(18, 1);
        await worker.RunOnceAsync(Ct);
        Assert.Equal(2, _store.Messages.Count);
    }

    private static DateTimeOffset Ist(int hour, int minute) => SchoolClock.At(Today, new TimeOnly(hour, minute));

    private void ApproveTemplate()
    {
        _store.GetTemplatesAsync(ClassId, Wording, Ct).GetAwaiter().GetResult();
        _store.ApproveAsync(ClassId, Guid.NewGuid(), Wording.Version, TeacherId, Ct).GetAwaiter().GetResult();
    }

    private void ConfirmLesson() =>
        _store.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today, ["M3.SUB.BORROW"]));

    private ApiFactory Create(bool demo = false) =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<IEveningStore>(_store);
                services.AddSingleton<IParentStore>(new FakeParentStore());
                services.AddSingleton<IAttendanceStore>(_attendance);
                services.AddSingleton<IAttendanceSource>(new NotYetSource());
                services.AddSingleton(Book);
                services.AddSingleton(Wording);
                services.AddSingleton<ILlmClient>(new ThrowingLlmClient());
                services.AddSingleton<TimeProvider>(_clock);
            },
            settings: new Dictionary<string, string?> { ["Demo:Enabled"] = demo.ToString() });

    private async Task<HttpResponseMessage> SendAsync(bool demo = false)
    {
        await using var factory = Create(demo);
        using var client = factory.CreateTeacherClient(ClassId);
        return await client.PostAsJsonAsync(SendUrl, new SendEveningRequest(Guid.NewGuid()), Ct);
    }

    private sealed class NotYetSource : IAttendanceSource
    {
        public Task<SourceResult> FetchAsync(
            Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster, CancellationToken cancellationToken) =>
            Task.FromResult<SourceResult>(new SourceResult.NotYet());
    }

    private sealed class ThrowingLlmClient : ILlmClient
    {
        public Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The evening message must not call an LLM (FR-20, guardrail 3).");
    }
}
