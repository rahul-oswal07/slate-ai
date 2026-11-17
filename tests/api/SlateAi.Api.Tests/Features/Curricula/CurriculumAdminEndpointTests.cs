using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Curricula;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Curricula;

/// <summary>T-147: review, publish and give out a curriculum over HTTP.</summary>
public sealed class CurriculumAdminEndpointTests
{
    private const string Url = "/api/admin/curricula";
    private static readonly Guid ClassId = Guid.NewGuid();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly FakeCurriculumAdminStore _store = new();
    private readonly Guid _id;

    public CurriculumAdminEndpointTests()
    {
        var draft = DraftRulesTests.Draft();
        _id = draft.Meta.Id;
        _store.Drafts[_id] = draft;
        _store.Classes.Add(new AdminClassChoice(ClassId, "Shobha · Grade 3", null));
    }

    [Fact]
    public async Task The_list_has_every_curriculum_and_class_and_a_curriculum_shows_answers_flags_and_why_it_can_t_be_published()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var list = (await client.GetFromJsonAsync<AdminCurricula>(Url, Ct))!;
        var view = (await client.GetFromJsonAsync<AdminCurriculumView>($"{Url}/{_id}", Ct))!;

        var summary = Assert.Single(list.Curricula);
        Assert.Equal((CurriculumStatuses.Draft, 2, 1, 1), (summary.Status, summary.Lessons, summary.NeedsReview, summary.Flagged));
        Assert.Equal("Shobha · Grade 3", Assert.Single(list.Classes).Label);
        Assert.Equal(2, list.BuiltInLessons);
        Assert.Equal(["M3.NUM.TENS", "M3.ADD.CARRY"], view.Lessons.Select(l => l.Lesson.Id));
        Assert.Equal(["A flag"], view.Lessons[1].Flags);
        Assert.Equal(["M3.ADD.CARRY: not reviewed yet."], view.PublishProblems);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{Guid.NewGuid()}", Ct)).StatusCode);
    }

    [Fact]
    public async Task An_edit_that_breaks_a_rule_is_400_by_field_and_a_good_one_returns_the_curriculum_with_code_s_answers()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var lesson = $"{Url}/{_id}/lessons/M3.ADD.CARRY";

        var bad = await client.PutAsJsonAsync(lesson, DraftRulesTests.Edit() with { Prerequisites = ["M3.ADD.CARRY"] }, Ct);
        var noRequest = await client.PutAsJsonAsync(lesson, DraftRulesTests.Edit() with { ClientRequestId = Guid.Empty }, Ct);
        var good = await client.PutAsJsonAsync(lesson, DraftRulesTests.Edit() with
        {
            Exercises = new ExerciseList(Add: [[248, 438]]),
            Reviewed = true,
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Contains("\"prerequisites\"", await bad.Content.ReadAsStringAsync(Ct));
        Assert.Equal(HttpStatusCode.BadRequest, noRequest.StatusCode);
        var view = (await good.Content.ReadFromJsonAsync<AdminCurriculumView>(Ct))!;
        Assert.Equal([686], view.Lessons[1].Answers.Add);
        Assert.Empty(view.PublishProblems);
    }

    [Fact]
    public async Task Publishing_a_draft_with_unreviewed_lessons_is_409_with_why_and_a_published_one_never_changes()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var early = await client.PostAsJsonAsync($"{Url}/{_id}/publish", new PublishCurriculumRequest(Guid.NewGuid()), Ct);
        await client.PutAsJsonAsync($"{Url}/{_id}/lessons/M3.ADD.CARRY", DraftRulesTests.Edit() with { Reviewed = true }, Ct);
        var published = await client.PostAsJsonAsync($"{Url}/{_id}/publish", new PublishCurriculumRequest(Guid.NewGuid()), Ct);
        var edit = await client.PutAsJsonAsync($"{Url}/{_id}/lessons/M3.ADD.CARRY", DraftRulesTests.Edit(), Ct);
        var remove = await client.DeleteAsync($"{Url}/{_id}/lessons/M3.ADD.CARRY?clientRequestId={Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal(["M3.ADD.CARRY: not reviewed yet."], (await early.Content.ReadFromJsonAsync<CurriculumProblems>(Ct))!.Problems);
        Assert.Equal(CurriculumStatuses.Published, (await published.Content.ReadFromJsonAsync<AdminCurriculumView>(Ct))!.Summary.Status);
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, remove.StatusCode);
    }

    [Fact]
    public async Task A_class_moves_only_to_a_published_curriculum_that_keeps_every_lesson_it_has_used()
    {
        _store.Used[ClassId] = ["M3.NUM.TENS"];
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var url = $"/api/admin/classes/{ClassId}/curriculum";

        var draft = await client.PutAsJsonAsync(url, new SetClassCurriculumRequest(Guid.NewGuid(), _id), Ct);
        _store.Drafts[_id].Meta.Status = CurriculumStatuses.Published;
        var moved = await client.PutAsJsonAsync(url, new SetClassCurriculumRequest(Guid.NewGuid(), _id), Ct);
        _store.Used[ClassId].Add("M3.SHAPE.ONLY.HERE");
        var back = await client.PutAsJsonAsync(url, new SetClassCurriculumRequest(Guid.NewGuid(), null), Ct);
        var unknown = await client.PutAsJsonAsync($"/api/admin/classes/{Guid.NewGuid()}/curriculum", new SetClassCurriculumRequest(Guid.NewGuid(), null), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, draft.StatusCode);
        Assert.Equal(_id, (await moved.Content.ReadFromJsonAsync<AdminClassChoice>(Ct))!.CurriculumId);
        Assert.Equal(HttpStatusCode.Conflict, back.StatusCode);
        Assert.Equal(["M3.SHAPE.ONLY.HERE"], (await back.Content.ReadFromJsonAsync<ClassCurriculumConflict>(Ct))!.Missing);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ICurriculumAdminStore>(_store);
            services.AddSingleton(new BuiltInCurriculum(new CurriculumGraph(
            [
                new Competency("M3.NUM.TENS", "ಹತ್ತು", "Tens and ones", [], null, 1, 1, [], null),
                new Competency("M3.ADD.CARRY", "ಕೂಡು", "Addition with carrying", [], null, 2, 2, [], null),
            ])));
        });
}
