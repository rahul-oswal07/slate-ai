using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Db;

/// <summary>
/// T-145: a class can follow a curriculum stored in Postgres. The jsonb lessons and chapters read back into a graph, the
/// class's lesson picker lists them, and a draft is never served.
/// </summary>
[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class CurriculumDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task A_class_following_a_published_curriculum_gets_its_chapters_and_lessons_in_order()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var published = await AddAsync(api, CurriculumStatuses.Published);
        await api.QueryAsync(async db =>
        {
            await db.Classes.Where(c => c.Id == api.ClassId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.CurriculumId, published), DemoClassApi.Ct);
            return 0;
        });

        var curriculum = await api.Client.GetFromJsonAsync<CurriculumResponse>(
            $"/api/classes/{api.ClassId}/curriculum", DemoClassApi.Ct);

        var chapter = Assert.Single(curriculum!.Chapters);
        Assert.Equal("Shapes", chapter.NameEn);
        Assert.Equal(["M3.SHAPE.SIDES", "M3.SHAPE.DIAGONAL"], chapter.Competencies.Select(c => c.Id));
        Assert.True(chapter.Competencies[0].HasExercises);
    }

    [Fact]
    public async Task A_draft_is_not_served_and_a_class_without_a_stored_curriculum_has_none()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var draft = await AddAsync(api, CurriculumStatuses.Draft);

        var (read, classCurriculum) = await api.QueryAsync(async db =>
        {
            var store = new CurriculumStore(db);
            return (await store.GetPublishedAsync(draft, DemoClassApi.Ct), await store.GetClassCurriculumIdAsync(api.ClassId, DemoClassApi.Ct));
        });

        Assert.Null(read);
        Assert.Null(classCurriculum);
    }

    private static async Task<Guid> AddAsync(DemoClassApi api, string status)
    {
        var id = Guid.NewGuid();
        Competency[] lessons =
        [
            // Stored out of order: the store reads them back in teaching order.
            new("M3.SHAPE.DIAGONAL", "ಕರ್ಣ", "Diagonals", ["M3.SHAPE.SIDES"], null, 1, 2, [], null),
            new("M3.SHAPE.SIDES", "ಬದಿಗಳು", "Sides and corners", [], null, 1, 1, [], null,
                Exercises: new ExerciseList(Compare: [[3, 4]])),
        ];
        await api.QueryAsync(async db =>
        {
            db.Curricula.Add(new StoredCurriculum
            {
                Id = id, Title = "Grade 3 maths (test)", Grade = 3, Subject = "maths", Medium = "kn", Status = status,
                Chapters = JsonSerializer.Serialize(new[] { new Chapter(1, "ಆಕೃತಿಗಳು", "Shapes") }, JsonSerializerOptions.Web),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            db.CurriculumLessons.AddRange(lessons.Select(l => new StoredCurriculumLesson
            {
                CurriculumId = id, CompetencyId = l.Id, Sequence = l.Sequence,
                Competency = JsonSerializer.Serialize(l, JsonSerializerOptions.Web),
            }));
            return await db.SaveChangesAsync(DemoClassApi.Ct);
        });
        return id;
    }
}
