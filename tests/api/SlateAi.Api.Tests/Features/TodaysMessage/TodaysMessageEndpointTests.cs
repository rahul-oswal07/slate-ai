using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Features.TodaysMessage;
using SlateAi.Api.Tests.Features.Attendance;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Features.Lessons;
using SlateAi.Api.Tests.Features.Parent;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.TodaysMessage;

/// <summary>Today's message over HTTP (T-88): approval bound to a version, sections, the evening send, the lesson hook.</summary>
public sealed class TodaysMessageEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 16);
    private static readonly string DayUrl = $"/api/classes/{ClassId}/todays-message/{Today:yyyy-MM-dd}";

    private static readonly EveningWording Wording = new("1", new Dictionary<string, LanguageWording>
    {
        ["kn"] = new("ಇಂದು {child} … {lesson} … {page}", "ಭಾಗ {part}, ಪುಟ {page}", "ಪುಟ {page}"),
        ["hi"] = new("आज {child} … {lesson} … {page}", "भाग {part}, पृष्ठ {page}", "पृष्ठ {page}"),
    });

    private static readonly CurriculumGraph Book = new([TodaysMessageTests.Compare, TodaysMessageTests.NoExercises], []);

    private static readonly EveningChild Manjunath = new(Guid.NewGuid(), 15, "Manjunath", "kn", "yes", false);
    private static readonly EveningChild Rohan = new(Guid.NewGuid(), 24, "Rohan", "hi", "yes", false);
    private static readonly EveningChild NoForm = new(Guid.NewGuid(), 2, "Bhavana", "kn", "no", false);
    private static readonly EveningChild Present = new(Guid.NewGuid(), 1, "Ananya", "kn", "yes", false);
    private static readonly EveningChild[] All = [Manjunath, Rohan, NoForm, Present];

    private readonly FakeEveningStore _evening = new(ClassId, TeacherId) { TeacherName = "Shobha" };
    private readonly FakeTodaysMessageStore _todays = new();
    private readonly FakeAttendanceStore _attendance = new(ClassId, All.Select(c => c.Id).ToList());
    private readonly FakeLessonStore _lessons = new();
    private readonly MovableClock _clock = new() { Now = SchoolClock.At(Today, new TimeOnly(15, 0)) };

    public TodaysMessageEndpointTests()
    {
        _evening.Children.AddRange(All);
        _evening.ApprovedTodaysMessage = date => _todays.ApprovedFor(ClassId, date);
        _todays.IsSent = id => _evening.Messages.Any(m => m.Message.TodaysMessageId == id);
        _todays.Answers = (_, _) => (_evening.PictureAnswer, _evening.Messages
            .Where(m => m.Message.TodaysMessageId is not null)
            .Select(m => (Message: m.Message, Child: _evening.Children.Single(c => c.Id == m.Message.ChildId)))
            .OrderBy(x => x.Child.RollNo)
            .Select(x => new TodaysAnswerRow(
                x.Child.Id, x.Child.FirstName, x.Child.RollNo,
                _evening.PictureTaps.GetValueOrDefault(x.Message.Token).First, _evening.PictureTaps.GetValueOrDefault(x.Message.Token).Second))
            .ToList());
        _attendance.SetDay(Today, All
            .Select(c => new AttendanceRow(c.Id, c == Present, AttendanceSources.Manual, null, null, null))
            .ToList());
        _evening.GetTemplatesAsync(ClassId, Wording, Ct).GetAwaiter().GetResult();
        _evening.ApproveAsync(ClassId, Guid.NewGuid(), Wording.Version, TeacherId, Ct).GetAwaiter().GetResult();
        _evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today, [TodaysMessageTests.Compare.Id]));
    }

    [Fact]
    public async Task No_content_until_a_draft_exists_then_shows_each_language_and_the_picture_by_code()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync(DayUrl, Ct)).StatusCode);
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());

        var view = (await client.GetFromJsonAsync<TodaysMessageView>(DayUrl, Ct))!;

        Assert.Equal((TodaysMessageStatus.Ready, 1, false), (view.Status, view.Version, view.Sent));
        Assert.Equal(new TodaysPicture("compare", 48, 33, ">"), view.Picture);
        Assert.Equal(1, view.Languages.Single(l => l.Language == "kn").Families); // Bhavana's form isn't back
        Assert.Equal(1, view.Languages.Single(l => l.Language == "hi").Families);
        Assert.StartsWith("/api/audio/", view.Languages[0].ConceptClipUrl);
        Assert.Empty(view.Removed);
    }

    [Fact]
    public async Task Approve_is_bound_to_the_version_she_read()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections(), version: 2);
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var stale = await client.PostAsJsonAsync($"{DayUrl}/approve", new ApproveTodaysMessageRequest(Guid.NewGuid(), 1), Ct);
        var approved = await client.PostAsJsonAsync($"{DayUrl}/approve", new ApproveTodaysMessageRequest(Guid.NewGuid(), 2), Ct);

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(TodaysMessageStatus.Ready, (await stale.Content.ReadFromJsonAsync<TodaysMessageView>(Ct))!.Status);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var view = (await approved.Content.ReadFromJsonAsync<TodaysMessageView>(Ct))!;
        Assert.Equal((TodaysMessageStatus.Approved, 2), (view.Status, view.ApprovedVersion));
        Assert.Empty(_evening.Messages); // 6 pm, not now: no send-on-approve outside the demo
    }

    [Fact]
    public async Task Removing_a_section_makes_a_new_version_and_she_must_approve_again()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        await client.PostAsJsonAsync($"{DayUrl}/approve", new ApproveTodaysMessageRequest(Guid.NewGuid(), 1), Ct);

        var removed = await client.PatchAsJsonAsync($"{DayUrl}/sections/activity", new SetTodaysSectionRequest(Guid.NewGuid(), true), Ct);
        var oldTick = await client.PostAsJsonAsync($"{DayUrl}/approve", new ApproveTodaysMessageRequest(Guid.NewGuid(), 1), Ct);

        var view = (await removed.Content.ReadFromJsonAsync<TodaysMessageView>(Ct))!;
        Assert.Equal((TodaysMessageStatus.Ready, 2, (int?)null), (view.Status, view.Version, view.ApprovedVersion));
        Assert.Equal(["activity"], view.Removed);
        Assert.Equal(HttpStatusCode.Conflict, oldTick.StatusCode);
    }

    [Theory]
    [InlineData("greeting", HttpStatusCode.BadRequest)]
    [InlineData("video", HttpStatusCode.Conflict)] // no video until T-94
    public async Task Unknown_or_missing_sections_are_refused(string kind, HttpStatusCode expected)
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await client.PatchAsJsonAsync($"{DayUrl}/sections/{kind}", new SetTodaysSectionRequest(Guid.NewGuid(), true), Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task At_6_pm_an_unapproved_message_never_goes_and_families_get_the_plain_one()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        _clock.Now = SchoolClock.At(Today, new TimeOnly(18, 30));

        await SendEveningAsync();

        Assert.Equal(2, _evening.Messages.Count);
        Assert.All(_evening.Messages, m => Assert.Null(m.Message.TodaysMessageId));
    }

    [Fact]
    public async Task At_6_pm_an_approved_message_goes_with_each_family_in_its_language_instead_of_the_lesson_clip()
    {
        var row = _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await ApproveAsync(1);
        _evening.ApprovedLessonAudio = _ => new Dictionary<string, Guid> { ["kn"] = Guid.NewGuid() };
        _clock.Now = SchoolClock.At(Today, new TimeOnly(18, 30));

        await SendEveningAsync();

        Assert.Equal(new[] { Manjunath.Id, Rohan.Id }.Order(), _evening.Messages.Select(m => m.Message.ChildId).Order());
        Assert.Equal(["hi", "kn"], _evening.Messages.Select(m => m.Message.Language).Order());
        Assert.All(_evening.Messages, m => Assert.Equal(row.Id, m.Message.TodaysMessageId));
        Assert.All(_evening.Messages, m => Assert.Null(m.Message.LessonAudioId));
    }

    [Fact]
    public async Task Once_sent_it_is_read_only()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await ApproveAsync(1);
        _clock.Now = SchoolClock.At(Today, new TimeOnly(18, 30));
        await SendEveningAsync();
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var remove = await client.PatchAsJsonAsync($"{DayUrl}/sections/concept", new SetTodaysSectionRequest(Guid.NewGuid(), true), Ct);
        var retry = await client.PostAsJsonAsync($"{DayUrl}/generate", new RetryTodaysMessageRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.Conflict, remove.StatusCode);
        Assert.True((await remove.Content.ReadFromJsonAsync<TodaysMessageView>(Ct))!.Sent);
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    public async Task In_the_demo_approve_sends_at_once(bool sendOnApprove, int sent)
    {
        var row = _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());

        await ApproveAsync(1, sendOnApprove);

        Assert.Equal(sent, _evening.Messages.Count);
        Assert.All(_evening.Messages, m => Assert.Equal(row.Id, m.Message.TodaysMessageId));
    }

    [Fact]
    public async Task The_family_page_carries_it_and_a_family_that_stopped_gets_nothing_of_it()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        _evening.TodaysMessage = TodaysFamilyView.For(TodaysMessageTests.Sections().WithRemoved(TodaysSectionKind.Activity, true)!, "kn");
        await ApproveAsync(1, sendOnApprove: true);
        var token = _evening.Messages.Single(m => m.Message.ChildId == Manjunath.Id).Message.Token;
        await using var factory = Create();
        using var anonymous = factory.CreateHttpsClient();

        var page = (await anonymous.GetFromJsonAsync<ParentEveningResponse>($"/api/p/{token}", Ct))!;

        Assert.Equal("ಕನ್ನಡ", page.TodaysMessage!.Concept);
        Assert.Null(page.TodaysMessage.Steps);
        Assert.Equal(48, page.TodaysMessage.Picture!.A);
    }

    [Fact]
    public async Task Confirming_a_lesson_with_the_books_exercises_starts_a_draft_once()
    {
        // A9 never answers here, so the draft stays generating and the replayed confirm finds it.
        await using var factory = Create(llm: new HangingLlmClient());
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        var url = $"/api/classes/{ClassId}/lessons/{Today:yyyy-MM-dd}";
        var request = new SaveLessonRequest(Guid.NewGuid(), [TodaysMessageTests.Compare.Id], LessonValues.Voice);

        (await client.PutAsJsonAsync(url, request, Ct)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync(url, request, Ct)).EnsureSuccessStatusCode();

        Assert.Equal(1, _todays.Starts);
        Assert.Equal([TodaysMessageTests.Compare.Id], _todays.Rows.Single().CompetencyIds);
    }

    [Fact]
    public async Task Confirming_a_lesson_without_exercises_starts_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        (await client.PutAsJsonAsync(
            $"/api/classes/{ClassId}/lessons/{Today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), [TodaysMessageTests.NoExercises.Id]),
            Ct)).EnsureSuccessStatusCode();

        Assert.Equal(0, _todays.Starts);
    }

    [Theory]
    [InlineData("Manjunath ಕಡ್ಡಿ ಕಟ್ಟು ಮಾಡಲಿ.", "name")]
    [InlineData("ಮೊದಲು 73 ನೋಡಿ.", "numbers")]
    [InlineData("ಒಂದು. ಎರಡು. ಮೂರು. ನಾಲ್ಕು.", "too-long")]
    public async Task Her_text_that_fails_a_check_is_not_saved_and_says_why(string text, string problem)
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await client.PutAsJsonAsync($"{DayUrl}/sections/concept", new EditTodaysSectionRequest(Guid.NewGuid(), 1, text), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains($"\"{problem}\"", body);
        Assert.Contains("\"48\"", body); // the numbers she may use
        Assert.DoesNotContain("Manjunath", body);
        var row = _todays.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Ready, 1, "ಕನ್ನಡ"), (row.Status, row.Version, row.Sections!.Concept.Text["kn"]));
    }

    [Fact]
    public async Task Her_edit_clears_the_approval_and_redrafts_from_her_kannada()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await ApproveAsync(1);
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await client.PutAsJsonAsync(
            $"{DayUrl}/sections/note", new EditTodaysSectionRequest(Guid.NewGuid(), 1, "ನಾವು ಎಲೆಗಳ ಮೇಲೆ 48 ಮತ್ತು 33 ಬರೆದು ಹೋಲಿಸಿದೆವು."), Ct);
        var stale = await client.PutAsJsonAsync($"{DayUrl}/sections/concept", new EditTodaysSectionRequest(Guid.NewGuid(), 0, "ಹೊಸದು."), Ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var row = _todays.Rows.Single();
        Assert.Null(row.ApprovedVersion);
        Assert.Equal("ನಾವು ಎಲೆಗಳ ಮೇಲೆ 48 ಮತ್ತು 33 ಬರೆದು ಹೋಲಿಸಿದೆವು.", row.Sections!.Note!.Text["kn"]);
        Assert.True(row.Sections.Teacher);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Another_pair_from_the_book_gets_its_answer_from_code_and_a_new_version()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await ApproveAsync(1);
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);

        var response = await client.PutAsJsonAsync($"{DayUrl}/picture", new SetTodaysPictureRequest(Guid.NewGuid(), 1, 3), Ct);
        var outside = await client.PutAsJsonAsync($"{DayUrl}/picture", new SetTodaysPictureRequest(Guid.NewGuid(), 2, 99), Ct);

        var view = (await response.Content.ReadFromJsonAsync<TodaysMessageView>(Ct))!;
        Assert.Equal(new TodaysPicture("compare", 86, 86, "="), view.Picture);
        Assert.Equal((2, (int?)null, 3, 4), (view.Version, view.ApprovedVersion, view.PictureIndex, view.Pairs.Count));
        Assert.Equal(HttpStatusCode.Conflict, outside.StatusCode);
    }

    [Fact]
    public async Task The_childs_taps_are_scored_by_code_with_one_more_try_and_the_answer_never_reaches_the_phone()
    {
        var token = await SentTokenAsync();
        await using var factory = Create();
        using var phone = factory.CreateHttpsClient();
        var url = $"/api/p/{token}/picture";

        var page = await phone.GetStringAsync($"/api/p/{token}", Ct);
        var first = await (await phone.PostAsJsonAsync(url, new PictureAnswerRequest(Guid.NewGuid(), "<"), Ct)).Content.ReadFromJsonAsync<PictureAnswerResponse>(Ct);
        var replayId = Guid.NewGuid();
        var second = await (await phone.PostAsJsonAsync(url, new PictureAnswerRequest(replayId, "="), Ct)).Content.ReadFromJsonAsync<PictureAnswerResponse>(Ct);
        var replay = await (await phone.PostAsJsonAsync(url, new PictureAnswerRequest(replayId, "="), Ct)).Content.ReadFromJsonAsync<PictureAnswerResponse>(Ct);
        var third = await phone.PostAsJsonAsync(url, new PictureAnswerRequest(Guid.NewGuid(), ">"), Ct);
        var after = (await phone.GetFromJsonAsync<ParentEveningResponse>($"/api/p/{token}", Ct))!;

        Assert.DoesNotContain("\"answer\"", page);
        Assert.Equal(new PictureAnswerResponse(false, 1, false), first);
        Assert.Equal(new PictureAnswerResponse(false, 2, true), second);
        Assert.Equal(second, replay);
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
        Assert.Equal(("<", "=", true), (after.TodaysMessage!.Picture!.FirstAnswer, after.TodaysMessage.Picture.SecondAnswer, after.TodaysMessage.Picture.Done));
    }

    [Fact]
    public async Task Right_first_time_is_done()
    {
        var token = await SentTokenAsync();
        await using var factory = Create();
        using var phone = factory.CreateHttpsClient();

        var first = await (await phone.PostAsJsonAsync($"/api/p/{token}/picture", new PictureAnswerRequest(Guid.NewGuid(), ">"), Ct))
            .Content.ReadFromJsonAsync<PictureAnswerResponse>(Ct);
        var again = await phone.PostAsJsonAsync($"/api/p/{token}/picture", new PictureAnswerRequest(Guid.NewGuid(), "<"), Ct);

        Assert.Equal(new PictureAnswerResponse(true, 1, true), first);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData("x", HttpStatusCode.BadRequest)]
    [InlineData("≥", HttpStatusCode.BadRequest)]
    public async Task Only_the_three_signs_are_taken(string sign, HttpStatusCode expected)
    {
        var token = await SentTokenAsync();
        await using var factory = Create();
        using var phone = factory.CreateHttpsClient();

        var response = await phone.PostAsJsonAsync($"/api/p/{token}/picture", new PictureAnswerRequest(Guid.NewGuid(), sign), Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task A_plain_message_or_a_removed_picture_has_no_question()
    {
        var token = await SentTokenAsync();
        _evening.TodaysMessage = _evening.TodaysMessage! with { Picture = null };
        await using var factory = Create();
        using var phone = factory.CreateHttpsClient();

        var response = await phone.PostAsJsonAsync($"/api/p/{token}/picture", new PictureAnswerRequest(Guid.NewGuid(), ">"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(new string[0], null, "notAnswered", 0)]
    [InlineData(new[] { ">" }, ">", "gotIt", 1)]
    [InlineData(new[] { "<" }, "<", "notAnswered", 0)] // one more try to come
    [InlineData(new[] { "<", ">" }, ">", "gettingThere", 1)]
    [InlineData(new[] { "<", "=" }, "=", "needsHelp", 1)]
    public async Task Today_shows_each_childs_answer_and_level_worked_out_by_code(string[] taps, string? answer, string level, int answered)
    {
        var token = await SentTokenAsync();
        await using var factory = Create();
        using var phone = factory.CreateHttpsClient();
        using var teacher = factory.CreateTeacherClient(ClassId, TeacherId);
        foreach (var sign in taps)
        {
            (await phone.PostAsJsonAsync($"/api/p/{token}/picture", new PictureAnswerRequest(Guid.NewGuid(), sign), Ct)).EnsureSuccessStatusCode();
        }

        var view = (await teacher.GetFromJsonAsync<TodaysAnswersView>($"{DayUrl}/answers", Ct))!;

        Assert.Equal((2, answered), (view.Sent, view.Answered)); // Manjunath (kn) and Rohan (hi)
        var row = view.Rows.Single(r => r.ChildId == Manjunath.Id);
        Assert.Equal(("Manjunath", answer, level), (row.FirstName, row.Answer, row.Level));
        Assert.Equal(TodaysLevel.NotAnswered, view.Rows.Single(r => r.ChildId == Rohan.Id).Level);
    }

    [Fact]
    public async Task Answers_are_204_without_a_todays_message_and_404_for_another_class()
    {
        await using var factory = Create();
        using var teacher = factory.CreateTeacherClient(ClassId, TeacherId);
        Assert.Equal(HttpStatusCode.NoContent, (await teacher.GetAsync($"{DayUrl}/answers", Ct)).StatusCode);

        await SentTokenAsync();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"{DayUrl}/answers", Ct)).StatusCode);
    }

    /// <summary>Approved in the demo, so it is sent at once; returns Manjunath's family's link.</summary>
    private async Task<string> SentTokenAsync()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        _evening.TodaysMessage = TodaysFamilyView.For(TodaysMessageTests.Sections(), "kn");
        await ApproveAsync(1, sendOnApprove: true);
        return _evening.Messages.Single(m => m.Message.ChildId == Manjunath.Id).Message.Token;
    }

    [Fact]
    public async Task Another_class_gets_404_and_signed_out_gets_401()
    {
        _todays.AddReady(ClassId, Today, TodaysMessageTests.Sections());
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(DayUrl, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync($"{DayUrl}/approve", new ApproveTodaysMessageRequest(Guid.NewGuid(), 1), Ct)).StatusCode);
    }

    private async Task ApproveAsync(int version, bool sendOnApprove = false)
    {
        await using var factory = Create(sendOnApprove);
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        (await client.PostAsJsonAsync($"{DayUrl}/approve", new ApproveTodaysMessageRequest(Guid.NewGuid(), version), Ct))
            .EnsureSuccessStatusCode();
    }

    private async Task SendEveningAsync()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        (await client.PostAsJsonAsync($"/api/classes/{ClassId}/evening-messages/send", new SendEveningRequest(Guid.NewGuid()), Ct))
            .EnsureSuccessStatusCode();
    }

    private ApiFactory Create(bool sendOnApprove = false, ILlmClient? llm = null) =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<IEveningStore>(_evening);
                services.AddSingleton<ITodaysMessageStore>(_todays);
                services.AddSingleton<ILessonStore>(_lessons);
                services.AddSingleton<IParentStore>(new FakeParentStore());
                services.AddSingleton<IAttendanceStore>(_attendance);
                services.AddSingleton<IAttendanceSource>(new NotYetSource());
                services.AddSingleton(Book);
                services.AddSingleton(Wording);
                services.AddSingleton<ILlmClient>(llm ?? new ThrowingLlmClient());
                services.AddSingleton<TimeProvider>(_clock);
            },
            settings: new Dictionary<string, string?>
            {
                ["Demo:Enabled"] = "true",
                ["Demo:SendOnApprove"] = sendOnApprove.ToString(),
            });

    private sealed class NotYetSource : IAttendanceSource
    {
        public Task<SourceResult> FetchAsync(
            Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster, CancellationToken cancellationToken) =>
            Task.FromResult<SourceResult>(new SourceResult.NotYet());
    }
}
