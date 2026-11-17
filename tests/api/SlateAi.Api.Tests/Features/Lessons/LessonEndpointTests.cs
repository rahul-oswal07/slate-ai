using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.TodaysMessage;
using SlateAi.Api.Tests.Curriculum;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Features.TodaysMessage;
using SlateAi.Api.Tests.Infrastructure;
using SlateAi.Api.Tests.Features.Attendance;

namespace SlateAi.Api.Tests.Features.Lessons;

public sealed class LessonEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 22);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly string Url = $"/api/classes/{ClassId}/lessons/{Today:yyyy-MM-dd}";

    private static readonly CurriculumGraph Book = new(
        [
            Item("M3.ADD.CARRY", chapter: 4, sequence: 1) with { Exercises = new ExerciseList(Add: [[248, 438]]) },
            Item("M3.SUB.NOBORROW", chapter: 5, sequence: 2),
            Item("M3.SUB.MAKE", chapter: 5, sequence: 3),
            Item("M3.SUB.BORROW", chapter: 5, sequence: 4),
        ],
        [new Chapter(4, "ಕೂಡಿ", "Adding"), new Chapter(5, "ಕಳೆಯುವ ಮೋಜು", "The fun of subtracting")]);

    private readonly FakeLessonStore _store = new();
    private readonly FakeEveningStore _evening = new(ClassId, Guid.NewGuid());

    [Fact]
    public async Task No_lesson_yet_suggests_the_first_lesson()
    {
        var day = await GetAsync();

        Assert.Null(day.Lesson);
        Assert.Equal("M3.ADD.CARRY", day.NextInBook);
    }

    [Fact]
    public async Task Next_in_the_book_follows_the_last_confirmed_lesson()
    {
        _store.Lessons[Today.AddDays(-1)] = new LessonRecord(["M3.SUB.NOBORROW", "M3.ADD.CARRY"], "picked", "confirmed");

        Assert.Equal("M3.SUB.MAKE", (await GetAsync()).NextInBook);
    }

    [Fact]
    public async Task Once_tonights_message_has_gone_another_lesson_is_refused_and_the_same_one_is_still_fine()
    {
        // T-143, D-124: families already have the message about this lesson; changing it would make Missed lessons
        // disagree with what they got, and the message can't be sent again.
        _store.Lessons[Today] = new LessonRecord(["M3.ADD.CARRY"], "picked", "confirmed");
        _evening.Messages.Add((Today, new NewEveningMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "kn", new SlotValues("", ""), "token", DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddDays(14))));

        var changed = await PutAsync(Guid.NewGuid(), "M3.SUB.MAKE");
        var same = await PutAsync(Guid.NewGuid(), "M3.ADD.CARRY");

        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        var current = await changed.Content.ReadFromJsonAsync<LessonDayResponse>(Ct);
        Assert.Equal(["M3.ADD.CARRY"], current!.Lesson!.CompetencyIds);
        Assert.True(current.SentToFamilies);
        Assert.Equal(["M3.ADD.CARRY"], _store.Lessons[Today].CompetencyIds);
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
    }

    [Fact]
    public async Task The_day_says_when_tonights_message_has_gone()
    {
        Assert.False((await GetAsync()).SentToFamilies);

        _evening.Messages.Add((Today, new NewEveningMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "kn", new SlotValues("", ""), "token", DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddDays(14))));

        Assert.True((await GetAsync()).SentToFamilies);
    }

    [Fact]
    public async Task Picking_saves_a_confirmed_picked_lesson_in_book_order()
    {
        var response = await PutAsync(Guid.NewGuid(), "M3.SUB.MAKE", "M3.SUB.NOBORROW");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var day = await response.Content.ReadFromJsonAsync<LessonDayResponse>(Ct);
        Assert.Equal(new[] { "M3.SUB.NOBORROW", "M3.SUB.MAKE" }, day!.Lesson!.CompetencyIds);
        Assert.Equal("picked", day.Lesson.Source);
        Assert.Equal("confirmed", day.Lesson.Status);
    }

    [Fact]
    public async Task Change_replaces_the_days_lesson_and_a_replay_is_a_no_op()
    {
        var first = Guid.NewGuid();
        await PutAsync(first, "M3.SUB.MAKE");
        await PutAsync(Guid.NewGuid(), "M3.SUB.BORROW");

        await PutAsync(first, "M3.SUB.MAKE");

        Assert.Equal(["M3.SUB.BORROW"], _store.Lessons[Today].CompetencyIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("M3.ADD.CARRY,M3.SUB.NOBORROW,M3.SUB.MAKE,M3.SUB.BORROW")]
    [InlineData("M9.NOT.REAL")]
    public async Task Rejects_none_more_than_three_or_unknown_lessons(string ids)
    {
        var response = await PutAsync(Guid.NewGuid(), ids.Split(',', StringSplitOptions.RemoveEmptyEntries));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_store.Lessons);
    }

    [Fact]
    public async Task Rejects_a_future_date_and_a_missing_request_id()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var future = await client.PutAsJsonAsync(
            $"/api/classes/{ClassId}/lessons/{Today.AddDays(1):yyyy-MM-dd}", new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.MAKE"]), Ct);
        var noId = await client.PutAsJsonAsync(Url, new SaveLessonRequest(Guid.Empty, ["M3.SUB.MAKE"]), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noId.StatusCode);
        Assert.Empty(_store.Lessons);
    }

    [Fact]
    public async Task Signed_out_is_401_and_another_class_is_404()
    {
        await using var factory = Create();
        using var anonymous = factory.CreateHttpsClient();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.PutAsJsonAsync(Url, new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.MAKE"]), Ct)).StatusCode);
    }

    [Fact]
    public async Task Curriculum_lists_chapters_with_titles_and_lessons_in_order()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var curriculum = await client.GetFromJsonAsync<CurriculumResponse>($"/api/classes/{ClassId}/curriculum", Ct);

        Assert.Equal([4, 5], curriculum!.Chapters.Select(c => c.Number));
        var five = curriculum.Chapters[1];
        Assert.Equal("The fun of subtracting", five.NameEn);
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.MAKE", "M3.SUB.BORROW"], five.Competencies.Select(c => c.Id));
    }

    [Fact]
    public async Task Curriculum_says_which_lessons_have_the_book_s_exercises()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var curriculum = await client.GetFromJsonAsync<CurriculumResponse>($"/api/classes/{ClassId}/curriculum", Ct);

        var lessons = curriculum!.Chapters.SelectMany(c => c.Competencies).ToDictionary(c => c.Id, c => c.HasExercises);
        Assert.True(lessons["M3.ADD.CARRY"]);
        Assert.False(lessons["M3.SUB.MAKE"]);
    }

    [Fact]
    public async Task A_class_that_follows_a_stored_curriculum_gets_its_lessons_and_can_only_pick_from_them()
    {
        var curricula = new FakeCurriculumStore();
        var stored = Guid.NewGuid();
        curricula.ClassCurricula[ClassId] = stored;
        curricula.Published[stored] = new CurriculumFile(
            3, "maths", null,
            [new Competency("M3.SHAPE.SIDES", "ಬದಿಗಳು", "Sides and corners", [], null, 1, 1, [], null)],
            [new Chapter(1, "ಆಕೃತಿಗಳು", "Shapes")]);
        await using var factory = Create(curricula);
        using var client = factory.CreateTeacherClient(ClassId);

        var curriculum = await client.GetFromJsonAsync<CurriculumResponse>($"/api/classes/{ClassId}/curriculum", Ct);
        var builtInLesson = await client.PutAsJsonAsync(Url, new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.MAKE"]), Ct);
        var ownLesson = await client.PutAsJsonAsync(Url, new SaveLessonRequest(Guid.NewGuid(), ["M3.SHAPE.SIDES"]), Ct);

        var chapter = Assert.Single(curriculum!.Chapters);
        Assert.Equal("Shapes", chapter.NameEn);
        Assert.Equal(["M3.SHAPE.SIDES"], chapter.Competencies.Select(c => c.Id));
        Assert.Equal(HttpStatusCode.BadRequest, builtInLesson.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownLesson.StatusCode);
    }

    [Fact]
    public async Task Curriculum_is_only_for_the_class_s_teachers()
    {
        await using var factory = Create();
        using var anonymous = factory.CreateHttpsClient();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/classes/{ClassId}/curriculum", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/classes/{ClassId}/curriculum", Ct)).StatusCode);
    }

    private ApiFactory Create(FakeCurriculumStore? curricula = null) =>
        FakeApi.Create(services =>
        {
            if (curricula is not null)
            {
                services.AddSingleton<ICurriculumStore>(curricula);
            }

            services.AddSingleton<ILessonStore>(_store);
            services.AddSingleton<ITodaysMessageStore>(new FakeTodaysMessageStore());
            services.AddSingleton<IEveningStore>(_evening);
            // Confirming any lesson now checks who is due today's message (T-139): an empty register, nobody due.
            services.AddSingleton<IAttendanceStore>(new FakeAttendanceStore(ClassId, []));
            services.AddSingleton(new BuiltInCurriculum(Book));
            services.AddSingleton<TimeProvider>(new FixedClock(SchoolClock.At(Today, new TimeOnly(15, 0))));
        });

    private async Task<LessonDayResponse> GetAsync()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        return (await client.GetFromJsonAsync<LessonDayResponse>(Url, Ct))!;
    }

    private async Task<HttpResponseMessage> PutAsync(Guid requestId, params string[] ids)
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        return await client.PutAsJsonAsync(Url, new SaveLessonRequest(requestId, ids), Ct);
    }

    private static Competency Item(string id, int chapter, int sequence) =>
        new(id, "ಕ", id, [], null, chapter, sequence, [], null);
}
