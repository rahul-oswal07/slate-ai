using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.LessonContents;

/// <summary>One lesson's stored description and examples, and where they came from (<see cref="LessonContentSources"/>).</summary>
public sealed record StoredLessonContent(string Description, IReadOnlyList<string> Examples, string Source);

/// <summary>One lesson of the content file, already checked by <see cref="LessonContentRules"/>.</summary>
public sealed record LessonContentEntry(string CompetencyId, LessonContentText Content, string Source);

/// <summary>What the startup load did.</summary>
public sealed record LessonContentSyncResult(int Added, int Updated, int KeptEdited);

/// <summary>Database side of the lesson descriptions (T-141), behind an interface so endpoints and the drafter can use a fake.</summary>
public interface ILessonContentStore
{
    /// <summary>Every lesson that has content: competency ID → content.</summary>
    Task<IReadOnlyDictionary<string, StoredLessonContent>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>The content of these lessons; lessons without any are left out.</summary>
    Task<IReadOnlyDictionary<string, StoredLessonContent>> GetForAsync(
        IReadOnlyList<string> competencyIds, CancellationToken cancellationToken);

    /// <summary>
    /// Sets (an edit on the admin page, never overwritten by the file again) or removes (null) the lesson's content,
    /// unless <paramref name="clientRequestId"/> was already applied. Returns the lesson's content afterwards.
    /// </summary>
    Task<StoredLessonContent?> SetAsync(
        Guid clientRequestId, string competencyId, LessonContentText? content, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// The startup load: adds the file's lessons that have no row and updates rows nobody edited when the file changed.
    /// A row edited on the admin page is left alone, and so is a lesson the file doesn't list.
    /// </summary>
    Task<LessonContentSyncResult> SyncAsync(
        IReadOnlyList<LessonContentEntry> entries, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class LessonContentStore(AppDbContext db) : ILessonContentStore
{
    private const string Endpoint = "admin-lesson-content";

    /// <summary>Arbitrary app-wide key: one startup load at a time when two instances start together.</summary>
    private const long SyncLockKey = 0x5EED_0141;

    public async Task<IReadOnlyDictionary<string, StoredLessonContent>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.LessonContents.AsNoTracking().ToDictionaryAsync(l => l.CompetencyId, ToStored, cancellationToken);

    public async Task<IReadOnlyDictionary<string, StoredLessonContent>> GetForAsync(
        IReadOnlyList<string> competencyIds, CancellationToken cancellationToken)
    {
        var ids = competencyIds.ToList();
        return await db.LessonContents.AsNoTracking()
            .Where(l => ids.Contains(l.CompetencyId))
            .ToDictionaryAsync(l => l.CompetencyId, ToStored, cancellationToken);
    }

    public async Task<StoredLessonContent?> SetAsync(
        Guid clientRequestId, string competencyId, LessonContentText? content, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"lesson-content:{competencyId}"}, 0))", cancellationToken);
        var row = await db.LessonContents.SingleOrDefaultAsync(l => l.CompetencyId == competencyId, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return row is null ? null : ToStored(row);
        }

        if (content is null)
        {
            if (row is not null)
            {
                db.LessonContents.Remove(row);
                row = null;
            }
        }
        else
        {
            if (row is null)
            {
                row = new LessonContent
                {
                    CompetencyId = competencyId, Description = "", Examples = [], Source = LessonContentSources.Edited,
                };
                db.LessonContents.Add(row);
            }

            row.Description = content.Description;
            row.Examples = [.. content.Examples];
            row.Source = LessonContentSources.Edited;
            row.UpdatedAt = now;
            row.EditedAt = now;
        }

        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = Endpoint, CreatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return row is null ? null : ToStored(row);
    }

    public async Task<LessonContentSyncResult> SyncAsync(
        IReadOnlyList<LessonContentEntry> entries, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({SyncLockKey})", cancellationToken);
        var rows = await db.LessonContents.ToDictionaryAsync(l => l.CompetencyId, cancellationToken);
        int added = 0, updated = 0, kept = 0;
        foreach (var entry in entries)
        {
            if (!rows.TryGetValue(entry.CompetencyId, out var row))
            {
                db.LessonContents.Add(new LessonContent
                {
                    CompetencyId = entry.CompetencyId,
                    Description = entry.Content.Description,
                    Examples = [.. entry.Content.Examples],
                    Source = entry.Source,
                    UpdatedAt = now,
                });
                added++;
            }
            else if (row.EditedAt is not null)
            {
                kept++;
            }
            else if (row.Description != entry.Content.Description
                || !row.Examples.SequenceEqual(entry.Content.Examples)
                || row.Source != entry.Source)
            {
                row.Description = entry.Content.Description;
                row.Examples = [.. entry.Content.Examples];
                row.Source = entry.Source;
                row.UpdatedAt = now;
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LessonContentSyncResult(added, updated, kept);
    }

    private static StoredLessonContent ToStored(LessonContent row) => new(row.Description, row.Examples, row.Source);
}
