using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Tests.Db;

/// <summary>
/// T-146: the import store on real Postgres. A replayed upload finds its import, a queued import is claimed once, steps
/// are saved for a resume, and the draft is written with every lesson needing review while the book is deleted;
/// discarding removes the import and its draft.
/// </summary>
[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class TextbookImportDbTests(PostgresFixture postgres)
{
    private static readonly ImportBook Book = new("Grade 3 maths", 3, "maths", "kn", "textbook", 1);

    [Fact]
    public async Task An_import_is_claimed_once_resumes_from_its_steps_and_becomes_a_draft_without_its_book()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var now = DateTimeOffset.UtcNow;
        var requestId = Guid.NewGuid();

        var (import, created) = await Store(api, s => s.CreateAsync(requestId, Book, [1, 2, 3], [2, 3], now, DemoClassApi.Ct));
        var (replay, again) = await Store(api, s => s.CreateAsync(requestId, Book, [9], [1], now, DemoClassApi.Ct));
        var claimed = await Store(api, s => s.TryStartAsync(import.Id, now, DemoClassApi.Ct));
        var second = await Store(api, s => s.TryStartAsync(import.Id, now, DemoClassApi.Ct));
        var stored = await Store(api, s => s.GetBookAsync(import.Id, DemoClassApi.Ct));
        await Store(api, async s => { await s.SaveStepAsync(import.Id, "outline:1-2", """{"pages":[]}""", 1, now, DemoClassApi.Ct); return 0; });
        var steps = await Store(api, s => s.GetStepsAsync(import.Id, DemoClassApi.Ct));

        Assert.True(created);
        Assert.False(again);
        Assert.Equal(import.Id, replay.Id);
        Assert.NotNull(claimed);
        Assert.Null(second);
        Assert.Equal([1, 2, 3], stored!.Value.Pdf);
        Assert.Equal([2, 3], stored.Value.PageSizes);
        Assert.Equal(2, replay.PageCount);
        Assert.Equal(["outline:1-2"], steps.Keys);

        var lesson = new DraftLesson(
            new Competency("M3.ADD.CARRY", "ಕೂಡು", "Addition", [], null, 1, 1, ["27 + 15 = ?"], new TextbookPage("textbook", 1, 34),
                Exercises: new ExerciseList(Add: [[248, 438]])),
            new LessonExtras([7], [34], "Adds with a carry.", ["36 + 47 is 83."], [new OtherExercise(7, 34, "Word problems")],
                [new PictureNote(7, 34, "top", "Bundles of ten sticks", "Tens")]),
            ["No printed page number was read for this lesson."]);
        var curriculumId = await Store(api, s => s.SaveDraftAsync(
            import.Id, new DraftCurriculum([new Chapter(1, "ಕೂಡುವುದು", "Addition")], [lesson]), ["A note"], now, DemoClassApi.Ct));
        var counts = await Store(api, s => s.CountDraftAsync(import.Id, DemoClassApi.Ct));

        var (saved, row, left) = await api.QueryAsync(async db => (
            await db.CurriculumImports.SingleAsync(i => i.Id == import.Id, DemoClassApi.Ct),
            await db.CurriculumLessons.SingleAsync(l => l.CurriculumId == curriculumId, DemoClassApi.Ct),
            await db.CurriculumImportFiles.CountAsync(f => f.ImportId == import.Id, DemoClassApi.Ct)));
        Assert.Equal((CurriculumImportStatuses.Ready, curriculumId, 0), (saved.Status, saved.CurriculumId, left));
        Assert.Equal(["A note"], saved.Notes);
        Assert.Equal(CurriculumReviewStatuses.NeedsReview, row.ReviewStatus);
        Assert.Equal(lesson.Flags, row.Flags);
        Assert.Equal("Bundles of ten sticks", JsonSerializer.Deserialize<LessonExtras>(row.Extras!, JsonSerializerOptions.Web)!.Pictures[0].Description);
        Assert.Equal((1, 1), counts);
        // A draft is not served to classes; publishing it is T-147.
        Assert.Null(await api.QueryAsync(db => new CurriculumStore(db).GetPublishedAsync(curriculumId, DemoClassApi.Ct)));

        Assert.True(await Store(api, s => s.DiscardAsync(import.Id, DemoClassApi.Ct)));
        Assert.Equal((0, 0), await api.QueryAsync(async db => (
            await db.CurriculumImports.CountAsync(i => i.Id == import.Id, DemoClassApi.Ct),
            await db.Curricula.CountAsync(c => c.Id == curriculumId, DemoClassApi.Ct))));
    }

    [Fact]
    public async Task A_failed_or_stalled_import_goes_back_in_the_queue_and_a_ready_one_does_not()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var now = DateTimeOffset.UtcNow;
        var (failed, _) = await Store(api, s => s.CreateAsync(Guid.NewGuid(), Book, [1], [1], now.AddHours(-1), DemoClassApi.Ct));
        var (stalled, _) = await Store(api, s => s.CreateAsync(Guid.NewGuid(), Book, [1], [1], now.AddHours(-1), DemoClassApi.Ct));
        var (moving, _) = await Store(api, s => s.CreateAsync(Guid.NewGuid(), Book, [1], [1], now, DemoClassApi.Ct));
        await Store(api, async s =>
        {
            await s.FailAsync(failed.Id, "error:Unavailable reading pages 1–1", now, DemoClassApi.Ct);
            await s.TryStartAsync(stalled.Id, now.AddHours(-1), DemoClassApi.Ct);
            await s.TryStartAsync(moving.Id, now, DemoClassApi.Ct);
            return 0;
        });

        var staleBefore = now.AddMinutes(-10);
        Assert.Equal(ImportRetry.Queued, await Store(api, s => s.RetryAsync(failed.Id, staleBefore, now, DemoClassApi.Ct)));
        Assert.Equal(ImportRetry.Queued, await Store(api, s => s.RetryAsync(stalled.Id, staleBefore, now, DemoClassApi.Ct)));
        Assert.Equal(ImportRetry.Unchanged, await Store(api, s => s.RetryAsync(moving.Id, staleBefore, now, DemoClassApi.Ct)));
        Assert.Equal(ImportRetry.NotFound, await Store(api, s => s.RetryAsync(Guid.NewGuid(), staleBefore, now, DemoClassApi.Ct)));
        Assert.Null((await Store(api, s => s.GetAsync(failed.Id, DemoClassApi.Ct)))!.Error);
    }

    private static Task<T> Store<T>(DemoClassApi api, Func<ITextbookImportStore, Task<T>> work) =>
        api.ScopedAsync<ITextbookImportStore, T>(work);
}
