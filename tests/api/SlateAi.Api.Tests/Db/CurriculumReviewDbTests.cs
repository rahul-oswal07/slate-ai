using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Curricula;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Tests.Db;

/// <summary>
/// T-147 on real Postgres: a draft is edited, loses a lesson (and that lesson from others' prerequisites), and is
/// published with its descriptions; a class that has taught lessons the curriculum lacks can't move to it, and a class
/// that hasn't can, and then reads it.
/// </summary>
[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class CurriculumReviewDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task A_draft_is_reviewed_published_with_its_descriptions_and_given_to_a_class_that_loses_nothing()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var now = DateTimeOffset.UtcNow;
        var id = await AddDraftAsync(api, now);

        // Edit: reviewed, with a description; then remove the third lesson, which the second one needed.
        foreach (var lesson in new[] { "M3.SHAPE.SIDES", "M3.SHAPE.DIAGONAL" })
        {
            var edit = new EditDraftLessonRequest(
                Guid.NewGuid(), "ಆಕೃತಿ", lesson == "M3.SHAPE.SIDES" ? "Sides" : "Diagonals", null, null, null, [], 12,
                lesson == "M3.SHAPE.DIAGONAL" ? ["M3.SHAPE.SIDES"] : [], null, null, [],
                "The child counts sides and corners.", ["A triangle has 3 sides."], [], [], Reviewed: true);
            Assert.Equal(DraftChange.Applied, (await Admin(api, s => s.EditLessonAsync(id, lesson, edit, now, DemoClassApi.Ct))).Change);
        }

        var notReady = await Admin(api, s => s.PublishAsync(id, Guid.NewGuid(), now, DemoClassApi.Ct));
        Assert.Equal(["M3.SHAPE.CORNERS: not reviewed yet."], notReady.Problems);
        Assert.Equal(DraftChange.Applied, await Admin(api, s => s.RemoveLessonAsync(id, "M3.SHAPE.CORNERS", Guid.NewGuid(), now, DemoClassApi.Ct)));

        var published = await Admin(api, s => s.PublishAsync(id, Guid.NewGuid(), now, DemoClassApi.Ct));
        Assert.Equal(DraftChange.Applied, published.Change);
        var draft = (await Admin(api, s => s.GetAsync(id, DemoClassApi.Ct)))!;
        Assert.Equal(CurriculumStatuses.Published, draft.Meta.Status);
        Assert.Equal(["M3.SHAPE.SIDES", "M3.SHAPE.DIAGONAL"], draft.Lessons.Select(l => l.Competency.Id));
        Assert.Equal(["M3.SHAPE.SIDES"], draft.Lessons[1].Competency.Prerequisites);
        var late = new EditDraftLessonRequest(Guid.NewGuid(), "x", "x", null, null, null, [], null, [], null, null, [], null, [], [], [], true);
        Assert.Equal(DraftChange.NotDraft, (await Admin(api, s => s.EditLessonAsync(id, "M3.SHAPE.SIDES", late, now, DemoClassApi.Ct))).Change);
        var contents = await api.ScopedAsync<ILessonContentStore, IReadOnlyDictionary<string, StoredLessonContent>>(
            s => s.GetAllAsync(id, DemoClassApi.Ct));
        Assert.Equal(["M3.SHAPE.DIAGONAL", "M3.SHAPE.SIDES"], contents.Keys.Order());

        // The demo class has taught a built-in lesson: moving it would lose it.
        var today = SchoolClock.Today(TimeProvider.System);
        (await api.Client.PutAsJsonAsync(
            $"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.ADD.CARRY"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var builtIn = new HashSet<string>(StringComparer.Ordinal);
        var (refused, missing) = await Admin(api, s => s.AssignAsync(api.ClassId, id, builtIn, Guid.NewGuid(), now, DemoClassApi.Ct));
        Assert.Equal(ClassAssign.WouldLoseLessons, refused);
        Assert.Equal(["M3.ADD.CARRY"], missing);

        // A new class has taught nothing yet: it moves, and reads the new curriculum.
        var newClass = Guid.NewGuid();
        await api.QueryAsync(async db =>
        {
            db.Classes.Add(new Class { Id = newClass, TeacherId = api.TeacherId, Grade = 3, Medium = "kn" });
            return await db.SaveChangesAsync(DemoClassApi.Ct);
        });
        Assert.Equal(ClassAssign.Applied, (await Admin(api, s => s.AssignAsync(newClass, id, builtIn, Guid.NewGuid(), now, DemoClassApi.Ct))).Result);
        var graph = await api.ScopedAsync<ICurriculumProvider, CurriculumGraph>(p => p.ForClassAsync(newClass, DemoClassApi.Ct));
        Assert.Equal(["M3.SHAPE.SIDES", "M3.SHAPE.DIAGONAL"], graph.All.Select(c => c.Id));
        Assert.Equal(id, await api.ScopedAsync<ICurriculumProvider, Guid>(p => p.IdForClassAsync(newClass, DemoClassApi.Ct)));
        Assert.Contains(await Admin(api, s => s.ListClassesAsync(DemoClassApi.Ct)), c => c.Id == newClass && c.CurriculumId == id);
    }

    private static async Task<Guid> AddDraftAsync(DemoClassApi api, DateTimeOffset now)
    {
        var id = Guid.NewGuid();
        Competency[] lessons =
        [
            new("M3.SHAPE.SIDES", "ಬದಿ", "Sides", [], null, 1, 1, [], null),
            new("M3.SHAPE.DIAGONAL", "ಕರ್ಣ", "Diagonals", ["M3.SHAPE.CORNERS"], null, 1, 3, [], null),
            new("M3.SHAPE.CORNERS", "ಮೂಲೆ", "Corners", [], null, 1, 2, [], null),
        ];
        await api.QueryAsync(async db =>
        {
            db.Curricula.Add(new StoredCurriculum
            {
                Id = id, Title = "Shapes", Grade = 3, Subject = "maths", Medium = "kn", Status = CurriculumStatuses.Draft,
                Chapters = JsonSerializer.Serialize(new[] { new Chapter(1, "ಆಕೃತಿ", "Shapes") }, JsonSerializerOptions.Web),
                CreatedAt = now,
            });
            db.CurriculumLessons.AddRange(lessons.Select(l => new StoredCurriculumLesson
            {
                CurriculumId = id, CompetencyId = l.Id, Sequence = l.Sequence, ReviewStatus = CurriculumReviewStatuses.NeedsReview,
                Competency = JsonSerializer.Serialize(l, JsonSerializerOptions.Web),
                Extras = JsonSerializer.Serialize(new LessonExtras([1], [12], null, [], [], []), JsonSerializerOptions.Web),
            }));
            return await db.SaveChangesAsync(DemoClassApi.Ct);
        });
        return id;
    }

    private static Task<T> Admin<T>(DemoClassApi api, Func<ICurriculumAdminStore, Task<T>> work) =>
        api.ScopedAsync<ICurriculumAdminStore, T>(work);
}
