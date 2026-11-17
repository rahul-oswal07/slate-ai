using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Features.Curricula;

public enum DraftChange
{
    Applied,
    NotFound,

    /// <summary>Published: it never changes again.</summary>
    NotDraft,

    /// <summary>Refused by <see cref="DraftRules"/>; nothing changed.</summary>
    Invalid,
}

/// <param name="Errors">An edit's errors by field.</param>
/// <param name="Problems">Why it can't be published.</param>
public sealed record DraftChangeResult(
    DraftChange Change, IReadOnlyDictionary<string, string[]>? Errors = null, IReadOnlyList<string>? Problems = null);

public enum ClassAssign
{
    Applied,
    ClassNotFound,
    CurriculumNotPublished,

    /// <summary>The class has used lessons the curriculum doesn't have.</summary>
    WouldLoseLessons,
}

/// <summary>A class an admin can give a curriculum to.</summary>
/// <param name="Label">"Shobha · Grade 3", as on the other admin pages.</param>
/// <param name="CurriculumId">The stored curriculum it follows; null = the built-in one.</param>
public sealed record AdminClassChoice(Guid Id, string Label, Guid? CurriculumId);

/// <summary>Database side of reviewing and publishing curricula (T-147), behind an interface so endpoints can use a fake.</summary>
public interface ICurriculumAdminStore
{
    /// <summary>Every stored curriculum, newest first, with its lessons.</summary>
    Task<IReadOnlyList<CurriculumDraft>> ListAsync(CancellationToken cancellationToken);

    Task<CurriculumDraft?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminClassChoice>> ListClassesAsync(CancellationToken cancellationToken);

    /// <summary>Applies <see cref="DraftRules.Apply"/> to a draft's lesson; replaying the request changes nothing.</summary>
    Task<DraftChangeResult> EditLessonAsync(
        Guid id, string competencyId, EditDraftLessonRequest edit, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Removes a lesson from a draft, and from every other lesson's prerequisites.</summary>
    Task<DraftChange> RemoveLessonAsync(
        Guid id, string competencyId, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a draft that passes <see cref="DraftRules.PublishProblems"/>: it never changes again, and its lessons'
    /// descriptions become the curriculum's <c>LessonContents</c>.
    /// </summary>
    Task<DraftChangeResult> PublishAsync(Guid id, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Gives the class a published curriculum, or the built-in one (null), unless that would lose lesson IDs it has
    /// already used: then <see cref="ClassAssign.WouldLoseLessons"/> with those IDs.
    /// </summary>
    /// <param name="builtInIds">The built-in curriculum's lesson IDs.</param>
    Task<(ClassAssign Result, IReadOnlyList<string> Missing)> AssignAsync(
        Guid classId, Guid? curriculumId, IReadOnlySet<string> builtInIds, Guid clientRequestId, DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed class CurriculumAdminStore(AppDbContext db) : ICurriculumAdminStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CurriculumDraft>> ListAsync(CancellationToken cancellationToken)
    {
        var ids = await db.Curricula.OrderByDescending(c => c.CreatedAt).Select(c => c.Id).ToListAsync(cancellationToken);
        var drafts = new List<CurriculumDraft>();
        foreach (var id in ids)
        {
            drafts.Add((await GetAsync(id, cancellationToken))!);
        }

        return drafts;
    }

    public async Task<CurriculumDraft?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.Curricula.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken) is not { } meta)
        {
            return null;
        }

        var import = meta.ImportId is { } importId
            ? await db.CurriculumImports.AsNoTracking().Where(i => i.Id == importId)
                .Select(i => new { i.Book, i.Part, i.Notes }).FirstOrDefaultAsync(cancellationToken)
            : null;
        var lessons = await db.CurriculumLessons.AsNoTracking().Where(l => l.CurriculumId == id).OrderBy(l => l.Sequence)
            .ToListAsync(cancellationToken);
        var classIds = await db.Classes.Where(c => c.CurriculumId == id).Select(c => c.Id).ToListAsync(cancellationToken);
        return new CurriculumDraft(
            meta,
            import?.Book ?? "textbook",
            import?.Part,
            import?.Notes ?? [],
            JsonSerializer.Deserialize<List<Chapter>>(meta.Chapters, Json) ?? [],
            lessons.Select(ToRow).ToList(),
            classIds);
    }

    public async Task<IReadOnlyList<AdminClassChoice>> ListClassesAsync(CancellationToken cancellationToken) =>
        await (
                from c in db.Classes.AsNoTracking()
                join t in db.Teachers on c.TeacherId equals t.Id
                orderby t.DisplayName, c.Grade
                select new AdminClassChoice(c.Id, t.DisplayName + " · Grade " + c.Grade, c.CurriculumId))
            .ToListAsync(cancellationToken);

    public async Task<DraftChangeResult> EditLessonAsync(
        Guid id, string competencyId, EditDraftLessonRequest edit, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(id, cancellationToken);
        if (await ReplayedAsync(edit.ClientRequestId, cancellationToken))
        {
            return new DraftChangeResult(DraftChange.Applied);
        }

        if (await GetAsync(id, cancellationToken) is not { } draft
            || draft.Lessons.FirstOrDefault(l => l.Competency.Id == competencyId) is not { } lesson)
        {
            return new DraftChangeResult(DraftChange.NotFound);
        }

        if (draft.Meta.Status != CurriculumStatuses.Draft)
        {
            return new DraftChangeResult(DraftChange.NotDraft);
        }

        var (edited, errors) = DraftRules.Apply(draft, lesson, edit);
        if (edited is null)
        {
            return new DraftChangeResult(DraftChange.Invalid, errors);
        }

        var row = await db.CurriculumLessons.SingleAsync(l => l.CurriculumId == id && l.CompetencyId == competencyId, cancellationToken);
        row.Competency = JsonSerializer.Serialize(edited.Competency, Json);
        row.Extras = JsonSerializer.Serialize(edited.Extras, Json);
        row.ReviewStatus = edited.ReviewStatus;
        await SaveAsync(edit.ClientRequestId, "admin-curriculum-lesson", now, transaction, cancellationToken);
        return new DraftChangeResult(DraftChange.Applied);
    }

    public async Task<DraftChange> RemoveLessonAsync(
        Guid id, string competencyId, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(id, cancellationToken);
        if (await ReplayedAsync(clientRequestId, cancellationToken))
        {
            return DraftChange.Applied;
        }

        if (await db.Curricula.FirstOrDefaultAsync(c => c.Id == id, cancellationToken) is not { } curriculum)
        {
            return DraftChange.NotFound;
        }

        if (curriculum.Status != CurriculumStatuses.Draft)
        {
            return DraftChange.NotDraft;
        }

        var rows = await db.CurriculumLessons.Where(l => l.CurriculumId == id).ToListAsync(cancellationToken);
        if (rows.FirstOrDefault(r => r.CompetencyId == competencyId) is not { } removed)
        {
            return DraftChange.NotFound;
        }

        db.CurriculumLessons.Remove(removed);
        foreach (var row in rows.Where(r => r != removed))
        {
            var competency = JsonSerializer.Deserialize<Competency>(row.Competency, Json)!;
            if (competency.Prerequisites.Contains(competencyId))
            {
                row.Competency = JsonSerializer.Serialize(
                    competency with { Prerequisites = competency.Prerequisites.Where(p => p != competencyId).ToList() }, Json);
            }
        }

        await SaveAsync(clientRequestId, "admin-curriculum-lesson-remove", now, transaction, cancellationToken);
        return DraftChange.Applied;
    }

    public async Task<DraftChangeResult> PublishAsync(Guid id, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(id, cancellationToken);
        if (await ReplayedAsync(clientRequestId, cancellationToken))
        {
            return new DraftChangeResult(DraftChange.Applied);
        }

        if (await GetAsync(id, cancellationToken) is not { } draft)
        {
            return new DraftChangeResult(DraftChange.NotFound);
        }

        if (draft.Meta.Status != CurriculumStatuses.Draft)
        {
            return new DraftChangeResult(DraftChange.NotDraft);
        }

        if (DraftRules.PublishProblems(draft) is { Count: > 0 } problems)
        {
            return new DraftChangeResult(DraftChange.Invalid, Problems: problems);
        }

        var curriculum = await db.Curricula.SingleAsync(c => c.Id == id, cancellationToken);
        curriculum.Status = CurriculumStatuses.Published;
        curriculum.PublishedAt = now;
        // Today's message builds its spoken concept on these (T-141), now for this curriculum's lessons (T-147).
        await db.LessonContents.Where(l => l.CurriculumId == id).ExecuteDeleteAsync(cancellationToken);
        foreach (var lesson in draft.Lessons)
        {
            var (content, _) = LessonContentRules.Normalise(lesson.Extras?.Description, [.. lesson.Extras?.Examples ?? []]);
            if (content is not null)
            {
                db.LessonContents.Add(new LessonContent
                {
                    CurriculumId = id,
                    CompetencyId = lesson.Competency.Id,
                    Description = content.Description,
                    Examples = [.. content.Examples],
                    Source = LessonContentSources.Textbook,
                    UpdatedAt = now,
                });
            }
        }

        await SaveAsync(clientRequestId, "admin-curriculum-publish", now, transaction, cancellationToken);
        return new DraftChangeResult(DraftChange.Applied);
    }

    public async Task<(ClassAssign Result, IReadOnlyList<string> Missing)> AssignAsync(
        Guid classId, Guid? curriculumId, IReadOnlySet<string> builtInIds, Guid clientRequestId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({"class-curriculum:" + classId}, 0))", cancellationToken);
        if (await ReplayedAsync(clientRequestId, cancellationToken))
        {
            return (ClassAssign.Applied, []);
        }

        if (await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken) is not { } group)
        {
            return (ClassAssign.ClassNotFound, []);
        }

        IReadOnlySet<string> target = builtInIds;
        if (curriculumId is { } id)
        {
            if (!await db.Curricula.AnyAsync(c => c.Id == id && c.Status == CurriculumStatuses.Published, cancellationToken))
            {
                return (ClassAssign.CurriculumNotPublished, []);
            }

            target = (await db.CurriculumLessons.Where(l => l.CurriculumId == id).Select(l => l.CompetencyId)
                .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        }

        // Every lesson ID the class has used: taught days, today's messages and catch-ups keep pointing at them.
        var used = (await db.Lessons.Where(l => l.ClassId == classId).Select(l => l.CompetencyIds).ToListAsync(cancellationToken))
            .Concat(await db.TodaysMessages.Where(t => t.ClassId == classId).Select(t => t.CompetencyIds).ToListAsync(cancellationToken))
            .Concat(await db.CatchUpPackages.Where(p => p.ClassId == classId).Select(p => p.TargetCompetencyIds).ToListAsync(cancellationToken))
            .Concat(await db.CatchUpAssignments.Where(a => a.ClassId == classId).Select(a => a.MissedCompetencyIds).ToListAsync(cancellationToken))
            .SelectMany(ids => ids);
        if (DraftRules.Missing(used, target) is { Count: > 0 } missing)
        {
            return (ClassAssign.WouldLoseLessons, missing);
        }

        group.CurriculumId = curriculumId;
        await SaveAsync(clientRequestId, "admin-class-curriculum", now, transaction, cancellationToken);
        return (ClassAssign.Applied, []);
    }

    private static DraftLessonRow ToRow(StoredCurriculumLesson row) => new(
        JsonSerializer.Deserialize<Competency>(row.Competency, Json)!,
        row.Extras is null ? null : JsonSerializer.Deserialize<LessonExtras>(row.Extras, Json),
        row.ReviewStatus,
        row.Flags);

    /// <summary>One change to a curriculum at a time: an edit racing a publish can't slip in after the check.</summary>
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> LockAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"curriculum:" + id}, 0))", cancellationToken);
        return transaction;
    }

    private Task<bool> ReplayedAsync(Guid clientRequestId, CancellationToken cancellationToken) =>
        db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken);

    private async Task SaveAsync(
        Guid clientRequestId, string endpoint, DateTimeOffset now,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = endpoint, CreatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
