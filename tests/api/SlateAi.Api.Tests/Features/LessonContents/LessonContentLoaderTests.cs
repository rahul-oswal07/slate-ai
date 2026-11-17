using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Tests.Features.LessonContents;

/// <summary>The startup load (T-141): new lessons in, unedited ones follow the file, an admin edit is kept.</summary>
public sealed class LessonContentLoaderTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private static LessonContentEntry Entry(string id, string description) =>
        new(id, new LessonContentText(description, ["An example."]), LessonContentSources.Textbook);

    [Fact]
    public async Task Adds_new_lessons_updates_unedited_ones_and_keeps_an_edit()
    {
        var store = new FakeLessonContentStore();
        await store.SyncAsync([Entry("M3.A", "First."), Entry("M3.B", "First.")], DateTimeOffset.UnixEpoch, Ct);
        await store.SetAsync(Guid.NewGuid(), BuiltInCurriculum.Id, "M3.B", new LessonContentText("Hers.", ["Her example."]), DateTimeOffset.UnixEpoch, Ct);

        var result = await store.SyncAsync(
            [Entry("M3.A", "Second."), Entry("M3.B", "Second."), Entry("M3.C", "New.")], DateTimeOffset.UnixEpoch, Ct);

        Assert.Equal(new LessonContentSyncResult(1, 1, 1), result);
        Assert.Equal("Second.", store.Contents["M3.A"].Description);
        Assert.Equal(("Hers.", LessonContentSources.Edited), (store.Contents["M3.B"].Description, store.Contents["M3.B"].Source));
    }

    [Fact]
    public void A_lesson_not_in_the_curriculum_or_with_a_bad_source_is_skipped_with_a_reason()
    {
        var curriculum = new SlateAi.Api.Curriculum.CurriculumGraph(
            [new SlateAi.Api.Curriculum.Competency("M3.A", "ಕ", "A", [], null, 1, 1, [], null)], []);
        var file = new LessonContentFile("v", [
            new LessonContentFile.Lesson("M3.A", "About.", ["One."], "textbook", [1]),
            new LessonContentFile.Lesson("M3.NOPE", "About.", ["One."], "textbook", null),
            new LessonContentFile.Lesson("M3.A", "About.", ["One."], "edited", null),
        ]);

        var (entries, problems) = file.Check(curriculum);

        Assert.Equal("M3.A", Assert.Single(entries).CompetencyId);
        Assert.Equal(2, problems.Count);
    }
}
