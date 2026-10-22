using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Lessons;

public sealed class LessonEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 22);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly string Url = $"/api/classes/{ClassId}/lessons/{Today:yyyy-MM-dd}";

    private static readonly CurriculumGraph Book = new(
        [
            Item("M3.ADD.CARRY", chapter: 4, sequence: 1),
            Item("M3.SUB.NOBORROW", chapter: 5, sequence: 2),
            Item("M3.SUB.CHECK", chapter: 5, sequence: 3),
            Item("M3.SUB.BORROW", chapter: 5, sequence: 4),
        ],
        [new Chapter(4, "ಕೂಡಿ", "Adding"), new Chapter(5, "ಕಳೆಯುವ ಮೋಜು", "The fun of subtracting")]);

    private readonly FakeLessonStore _store = new();

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

        Assert.Equal("M3.SUB.CHECK", (await GetAsync()).NextInBook);
    }

    [Fact]
    public async Task Picking_saves_a_confirmed_picked_lesson_in_book_order()
    {
        var response = await PutAsync(Guid.NewGuid(), "M3.SUB.CHECK", "M3.SUB.NOBORROW");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var day = await response.Content.ReadFromJsonAsync<LessonDayResponse>(Ct);
        Assert.Equal(new[] { "M3.SUB.NOBORROW", "M3.SUB.CHECK" }, day!.Lesson!.CompetencyIds);
        Assert.Equal("picked", day.Lesson.Source);
        Assert.Equal("confirmed", day.Lesson.Status);
    }

    [Fact]
    public async Task Change_replaces_the_days_lesson_and_a_replay_is_a_no_op()
    {
        var first = Guid.NewGuid();
        await PutAsync(first, "M3.SUB.CHECK");
        await PutAsync(Guid.NewGuid(), "M3.SUB.BORROW");

        await PutAsync(first, "M3.SUB.CHECK");

        Assert.Equal(["M3.SUB.BORROW"], _store.Lessons[Today].CompetencyIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("M3.ADD.CARRY,M3.SUB.NOBORROW,M3.SUB.CHECK,M3.SUB.BORROW")]
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
            $"/api/classes/{ClassId}/lessons/{Today.AddDays(1):yyyy-MM-dd}", new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.CHECK"]), Ct);
        var noId = await client.PutAsJsonAsync(Url, new SaveLessonRequest(Guid.Empty, ["M3.SUB.CHECK"]), Ct);

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
            (await other.PutAsJsonAsync(Url, new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.CHECK"]), Ct)).StatusCode);
    }

    [Fact]
    public async Task Curriculum_lists_chapters_with_titles_and_lessons_in_order()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var curriculum = await client.GetFromJsonAsync<CurriculumResponse>("/api/curriculum", Ct);

        Assert.Equal([4, 5], curriculum!.Chapters.Select(c => c.Number));
        var five = curriculum.Chapters[1];
        Assert.Equal("The fun of subtracting", five.NameEn);
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.CHECK", "M3.SUB.BORROW"], five.Competencies.Select(c => c.Id));
    }

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ILessonStore>(_store);
            services.AddSingleton(Book);
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
