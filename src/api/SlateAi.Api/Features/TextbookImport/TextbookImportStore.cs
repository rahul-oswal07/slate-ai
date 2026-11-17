using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>What happened to a retry.</summary>
public enum ImportRetry
{
    NotFound,

    /// <summary>Back in the queue.</summary>
    Queued,

    /// <summary>Already queued or running and not stale, or already ready: nothing to do.</summary>
    Unchanged,
}

/// <summary>Database side of textbook imports (T-146), behind an interface so the job and endpoints can use a fake.</summary>
public interface ITextbookImportStore
{
    /// <summary>
    /// Saves a new import with its pages as <see cref="CurriculumImportStatuses.Queued"/>, unless an import with this
    /// <paramref name="clientRequestId"/> exists: then that one (a replayed upload). <c>Created</c> says which.
    /// </summary>
    Task<(CurriculumImport Import, bool Created)> CreateAsync(
        Guid clientRequestId, ImportBook book, IReadOnlyList<byte[]> pages, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>The import made by this upload request, if any.</summary>
    Task<CurriculumImport?> FindByRequestAsync(Guid clientRequestId, CancellationToken cancellationToken);

    /// <summary>Every import, newest first.</summary>
    Task<IReadOnlyList<CurriculumImport>> ListAsync(CancellationToken cancellationToken);

    Task<CurriculumImport?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lessons in the import's draft, and how many have flags; null without a draft.</summary>
    Task<(int Lessons, int Flagged)?> CountDraftAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Moves a queued import to running and returns it; null if it isn't queued (another run has it, or it's done).</summary>
    Task<CurriculumImport?> TryStartAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>The size in bytes of each page's PDF, in page order.</summary>
    Task<IReadOnlyList<int>> GetPageSizesAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The pages' PDFs, in the order asked.</summary>
    Task<IReadOnlyList<byte[]>> GetPagesAsync(Guid id, IReadOnlyList<int> numbers, CancellationToken cancellationToken);

    /// <summary>Finished steps by key, as saved.</summary>
    Task<IReadOnlyDictionary<string, string>> GetStepsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Saves a finished step and moves the progress on.</summary>
    Task SaveStepAsync(Guid id, string key, string result, int stepsDone, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Sets the stage and progress (and so <c>UpdatedAt</c>).</summary>
    Task SetProgressAsync(Guid id, string stage, int stepsDone, int stepsTotal, DateTimeOffset now, CancellationToken cancellationToken);

    Task FailAsync(Guid id, string error, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// In one transaction: writes the draft curriculum (every lesson needs review), marks the import ready with it, and
    /// deletes the book's pages (D-125). Returns the draft's ID.
    /// </summary>
    /// <param name="notes">About the whole book, for the review page.</param>
    Task<Guid> SaveDraftAsync(
        Guid id, DraftCurriculum draft, IReadOnlyList<string> notes, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Back to queued if it failed, or ran but stopped moving before <paramref name="staleBefore"/>.</summary>
    Task<ImportRetry> RetryAsync(Guid id, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the import, its pages and steps, and its draft unless the draft was published (T-147). False if there was
    /// no such import.
    /// </summary>
    Task<bool> DiscardAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class TextbookImportStore(AppDbContext db) : ITextbookImportStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<(CurriculumImport Import, bool Created)> CreateAsync(
        Guid clientRequestId, ImportBook book, IReadOnlyList<byte[]> pages, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({"curriculum-import-request:" + clientRequestId}, 0))", cancellationToken);
        if (await db.CurriculumImports.AsNoTracking().FirstOrDefaultAsync(i => i.ClientRequestId == clientRequestId, cancellationToken) is { } existing)
        {
            return (existing, false);
        }

        var import = new CurriculumImport
        {
            Id = Guid.NewGuid(),
            ClientRequestId = clientRequestId,
            Title = book.Title,
            Grade = book.Grade,
            Subject = book.Subject,
            Medium = book.Medium,
            Book = book.Book,
            Part = book.Part,
            PageCount = pages.Count,
            Status = CurriculumImportStatuses.Queued,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CurriculumImports.Add(import);
        db.CurriculumImportPages.AddRange(pages.Select((pdf, i) => new CurriculumImportPage { ImportId = import.Id, Number = i + 1, Pdf = pdf }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear(); // the pages' bytes stay out of memory
        return (import, true);
    }

    public Task<CurriculumImport?> FindByRequestAsync(Guid clientRequestId, CancellationToken cancellationToken) =>
        db.CurriculumImports.AsNoTracking().FirstOrDefaultAsync(i => i.ClientRequestId == clientRequestId, cancellationToken);

    public async Task<IReadOnlyList<CurriculumImport>> ListAsync(CancellationToken cancellationToken) =>
        await db.CurriculumImports.AsNoTracking().OrderByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);

    public Task<CurriculumImport?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.CurriculumImports.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<(int Lessons, int Flagged)?> CountDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.CurriculumImports.Where(i => i.Id == id).Select(i => i.CurriculumId).FirstOrDefaultAsync(cancellationToken) is not { } curriculumId)
        {
            return null;
        }

        var lessons = db.CurriculumLessons.Where(l => l.CurriculumId == curriculumId);
        return (await lessons.CountAsync(cancellationToken), await lessons.CountAsync(l => l.Flags.Length > 0, cancellationToken));
    }

    public async Task<CurriculumImport?> TryStartAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var started = await db.CurriculumImports
            .Where(i => i.Id == id && i.Status == CurriculumImportStatuses.Queued)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Status, CurriculumImportStatuses.Running)
                .SetProperty(i => i.Error, (string?)null)
                .SetProperty(i => i.UpdatedAt, now), cancellationToken);
        return started == 1 ? await GetAsync(id, cancellationToken) : null;
    }

    public async Task<IReadOnlyList<int>> GetPageSizesAsync(Guid id, CancellationToken cancellationToken) =>
        await db.CurriculumImportPages.Where(p => p.ImportId == id).OrderBy(p => p.Number)
            .Select(p => p.Pdf.Length).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<byte[]>> GetPagesAsync(Guid id, IReadOnlyList<int> numbers, CancellationToken cancellationToken)
    {
        var pages = await db.CurriculumImportPages.AsNoTracking()
            .Where(p => p.ImportId == id && numbers.Contains(p.Number))
            .ToDictionaryAsync(p => p.Number, p => p.Pdf, cancellationToken);
        return numbers.Select(n => pages[n]).ToList();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetStepsAsync(Guid id, CancellationToken cancellationToken) =>
        await db.CurriculumImportSteps.AsNoTracking().Where(s => s.ImportId == id)
            .ToDictionaryAsync(s => s.Key, s => s.Result, cancellationToken);

    public async Task SaveStepAsync(Guid id, string key, string result, int stepsDone, DateTimeOffset now, CancellationToken cancellationToken)
    {
        db.CurriculumImportSteps.Add(new CurriculumImportStep { ImportId = id, Key = key, Result = result, CreatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        await db.CurriculumImports.Where(i => i.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(i => i.StepsDone, stepsDone)
            .SetProperty(i => i.UpdatedAt, now), cancellationToken);
    }

    public Task SetProgressAsync(Guid id, string stage, int stepsDone, int stepsTotal, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.CurriculumImports.Where(i => i.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(i => i.Stage, stage)
            .SetProperty(i => i.StepsDone, stepsDone)
            .SetProperty(i => i.StepsTotal, stepsTotal)
            .SetProperty(i => i.UpdatedAt, now), cancellationToken);

    public Task FailAsync(Guid id, string error, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.CurriculumImports.Where(i => i.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(i => i.Status, CurriculumImportStatuses.Failed)
            .SetProperty(i => i.Error, error.Length > 200 ? error[..200] : error)
            .SetProperty(i => i.UpdatedAt, now), cancellationToken);

    public async Task<Guid> SaveDraftAsync(
        Guid id, DraftCurriculum draft, IReadOnlyList<string> notes, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var import = await db.CurriculumImports.SingleAsync(i => i.Id == id, cancellationToken);
        var curriculum = new StoredCurriculum
        {
            Id = Guid.NewGuid(),
            Title = import.Title,
            Grade = import.Grade,
            Subject = import.Subject,
            Medium = import.Medium,
            Status = CurriculumStatuses.Draft,
            Chapters = JsonSerializer.Serialize(draft.Chapters, Json),
            CreatedAt = now,
            ImportId = id,
        };
        db.Curricula.Add(curriculum);
        db.CurriculumLessons.AddRange(draft.Lessons.Select(l => new StoredCurriculumLesson
        {
            CurriculumId = curriculum.Id,
            CompetencyId = l.Competency.Id,
            Sequence = l.Competency.Sequence,
            Competency = JsonSerializer.Serialize(l.Competency, Json),
            ReviewStatus = CurriculumReviewStatuses.NeedsReview,
            Flags = [.. l.Flags],
            Extras = JsonSerializer.Serialize(l.Extras, Json),
        }));
        import.Status = CurriculumImportStatuses.Ready;
        import.Stage = null;
        import.Error = null;
        import.CurriculumId = curriculum.Id;
        import.Notes = [.. notes];
        import.StepsDone = import.StepsTotal;
        import.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        // The book's pages aren't needed once the draft is written, and we don't keep the book (D-125).
        await db.CurriculumImportPages.Where(p => p.ImportId == id).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return curriculum.Id;
    }

    public async Task<ImportRetry> RetryAsync(Guid id, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!await db.CurriculumImports.AnyAsync(i => i.Id == id, cancellationToken))
        {
            return ImportRetry.NotFound;
        }

        var queued = await db.CurriculumImports
            .Where(i => i.Id == id && (i.Status == CurriculumImportStatuses.Failed
                                       || (i.Status == CurriculumImportStatuses.Running && i.UpdatedAt < staleBefore)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Status, CurriculumImportStatuses.Queued)
                .SetProperty(i => i.Error, (string?)null)
                .SetProperty(i => i.UpdatedAt, now), cancellationToken);
        return queued == 1 ? ImportRetry.Queued : ImportRetry.Unchanged;
    }

    public async Task<bool> DiscardAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.CurriculumImports.FirstOrDefaultAsync(i => i.Id == id, cancellationToken) is not { } import)
        {
            return false;
        }

        // Pages and steps go with the import (cascade); a draft goes too, but never a published curriculum (T-147).
        await db.CurriculumImports.Where(i => i.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (import.CurriculumId is { } curriculumId)
        {
            await db.Curricula.Where(c => c.Id == curriculumId && c.Status == CurriculumStatuses.Draft).ExecuteDeleteAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
