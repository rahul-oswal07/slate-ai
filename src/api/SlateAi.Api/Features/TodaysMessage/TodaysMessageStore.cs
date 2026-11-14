using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>One today's message as the service reads it. <see cref="Sections"/> is null until the first draft is ready.</summary>
/// <param name="Sent">Families already have it (an evening message carries it): it can't change any more.</param>
public sealed record TodaysMessageRow(
    Guid Id,
    Guid ClassId,
    DateOnly Date,
    IReadOnlyList<string> CompetencyIds,
    string Status,
    int Version,
    int? ApprovedVersion,
    DateTimeOffset? ApprovedAt,
    string? Verdict,
    TodaysSections? Sections,
    DateTimeOffset UpdatedAt,
    bool Sent);

/// <summary>One family that got today's message, with the child's taps on the picture question (T-90).</summary>
public sealed record TodaysAnswerRow(Guid ChildId, string FirstName, int RollNo, string? First, string? Second);

public enum TodaysChange
{
    Applied,

    /// <summary>This clientRequestId was already applied: nothing changed.</summary>
    Replay,

    /// <summary>Not allowed now (not ready, another version, already sent, last section): show the latest.</summary>
    Conflict,

    NotFound,
}

/// <summary>Database side of today's message (T-88), behind an interface so endpoint tests use a fake.</summary>
public interface ITodaysMessageStore
{
    Task<TodaysMessageRow?> GetAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    Task<TodaysMessageRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Puts the day's message back to generating for <paramref name="competencyIds"/> (creating it if needed) and clears
    /// any approval; null when it was already sent, which nothing can change.
    /// </summary>
    Task<Guid?> StartAsync(Guid classId, DateOnly date, IReadOnlyList<string> competencyIds, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>A new draft: ready at version + 1, approval cleared.</summary>
    Task SetReadyAsync(Guid id, TodaysSections sections, string verdict, DateTimeOffset now, CancellationToken cancellationToken);

    Task SetFailedAsync(Guid id, string verdict, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Approves exactly <paramref name="version"/> of a ready, unsent message (guardrail 3).</summary>
    Task<TodaysChange> ApproveAsync(
        Guid classId, DateOnly date, Guid clientRequestId, int version, Guid teacherId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Removes or restores one section: a new version, back to ready, approval cleared (she ticks again).</summary>
    Task<TodaysChange> SetRemovedAsync(
        Guid classId, DateOnly date, Guid clientRequestId, string kind, bool removed, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Applies her change to the sections of <paramref name="version"/> (ready or approved, not sent): with
    /// <paramref name="redraft"/> it goes back to generating (A9 and the voice run again, then ready at version + 1); without,
    /// it is ready at version + 1 at once. Either way her approval is cleared (D-92). Conflict when <paramref name="change"/>
    /// returns null.
    /// </summary>
    Task<TodaysChange> ReviseAsync(
        Guid classId,
        DateOnly date,
        Guid clientRequestId,
        int version,
        Func<TodaysSections, TodaysSections?> change,
        bool redraft,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// The families the day's message went to, with the taps, and the picture's answer to score them by (T-92); null when
    /// the class has no today's message that day.
    /// </summary>
    Task<(string Answer, IReadOnlyList<TodaysAnswerRow> Rows)?> GetAnswersAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>The day's message when it is approved at its current version: the only one the evening send attaches (guardrail 3).</summary>
    Task<TodaysMessageRow?> GetApprovedAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);
}

public sealed class TodaysMessageStore(AppDbContext db) : ITodaysMessageStore
{
    private const string ApproveEndpoint = "todays-message-approve";
    private const string SectionEndpoint = "todays-message-section";

    public async Task<TodaysMessageRow?> GetAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        await RowAsync(db.TodaysMessages.Where(t => t.ClassId == classId && t.Date == date), cancellationToken);

    public async Task<TodaysMessageRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await RowAsync(db.TodaysMessages.Where(t => t.Id == id), cancellationToken);

    public async Task<Guid?> StartAsync(
        Guid classId, DateOnly date, IReadOnlyList<string> competencyIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(classId, date, cancellationToken);
        var row = await db.TodaysMessages.SingleOrDefaultAsync(t => t.ClassId == classId && t.Date == date, cancellationToken);
        if (row is null)
        {
            row = new Data.Entities.TodaysMessage
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                Date = date,
                Status = TodaysMessageStatus.Generating,
                Sections = "null",
                CreatedAt = now,
            };
            db.TodaysMessages.Add(row);
        }
        else if (await SentAsync(row.Id, cancellationToken))
        {
            return null;
        }

        if (!row.CompetencyIds.SequenceEqual(competencyIds))
        {
            // Another lesson: nothing of the old draft (or her edits to it) carries over.
            row.Sections = "null";
        }

        row.CompetencyIds = [.. competencyIds];
        row.Status = TodaysMessageStatus.Generating;
        ClearApproval(row);
        row.Verdict = null;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return row.Id;
    }

    public async Task SetReadyAsync(Guid id, TodaysSections sections, string verdict, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.TodaysMessages.Where(t => t.Id == id).ExecuteUpdateAsync(
            s => s.SetProperty(t => t.Status, TodaysMessageStatus.Ready)
                .SetProperty(t => t.Sections, sections.ToJson())
                .SetProperty(t => t.Version, t => t.Version + 1)
                .SetProperty(t => t.Verdict, verdict)
                .SetProperty(t => t.ApprovedVersion, (int?)null)
                .SetProperty(t => t.ApprovedAt, (DateTimeOffset?)null)
                .SetProperty(t => t.ApprovedByTeacherId, (Guid?)null)
                .SetProperty(t => t.UpdatedAt, now),
            cancellationToken);

    public async Task SetFailedAsync(Guid id, string verdict, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.TodaysMessages.Where(t => t.Id == id).ExecuteUpdateAsync(
            s => s.SetProperty(t => t.Status, TodaysMessageStatus.Failed)
                .SetProperty(t => t.Verdict, verdict)
                .SetProperty(t => t.UpdatedAt, now),
            cancellationToken);

    public async Task<TodaysChange> ApproveAsync(
        Guid classId, DateOnly date, Guid clientRequestId, int version, Guid teacherId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(classId, date, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return TodaysChange.Replay;
        }

        if (await db.TodaysMessages.SingleOrDefaultAsync(t => t.ClassId == classId && t.Date == date, cancellationToken) is not { } row)
        {
            return TodaysChange.NotFound;
        }

        if (row.Status != TodaysMessageStatus.Ready || row.Version != version || await SentAsync(row.Id, cancellationToken))
        {
            return TodaysChange.Conflict;
        }

        row.Status = TodaysMessageStatus.Approved;
        row.ApprovedVersion = version;
        row.ApprovedAt = now;
        row.ApprovedByTeacherId = teacherId;
        row.UpdatedAt = now;
        Processed(clientRequestId, ApproveEndpoint, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TodaysChange.Applied;
    }

    public async Task<TodaysChange> SetRemovedAsync(
        Guid classId, DateOnly date, Guid clientRequestId, string kind, bool removed, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(classId, date, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return TodaysChange.Replay;
        }

        if (await db.TodaysMessages.SingleOrDefaultAsync(t => t.ClassId == classId && t.Date == date, cancellationToken) is not { } row)
        {
            return TodaysChange.NotFound;
        }

        if (row.Status is not (TodaysMessageStatus.Ready or TodaysMessageStatus.Approved)
            || TodaysSections.Parse(row.Sections) is not { } sections
            || sections.WithRemoved(kind, removed) is not { HasAnySection: true } changed
            || await SentAsync(row.Id, cancellationToken))
        {
            return TodaysChange.Conflict;
        }

        row.Sections = changed.ToJson();
        row.Status = TodaysMessageStatus.Ready;
        row.Version++;
        ClearApproval(row);
        row.UpdatedAt = now;
        Processed(clientRequestId, SectionEndpoint, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TodaysChange.Applied;
    }

    public async Task<TodaysChange> ReviseAsync(
        Guid classId,
        DateOnly date,
        Guid clientRequestId,
        int version,
        Func<TodaysSections, TodaysSections?> change,
        bool redraft,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(classId, date, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return TodaysChange.Replay;
        }

        if (await db.TodaysMessages.SingleOrDefaultAsync(t => t.ClassId == classId && t.Date == date, cancellationToken) is not { } row)
        {
            return TodaysChange.NotFound;
        }

        if (row.Version != version
            || row.Status is not (TodaysMessageStatus.Ready or TodaysMessageStatus.Approved)
            || TodaysSections.Parse(row.Sections) is not { } sections
            || change(sections) is not { } changed
            || await SentAsync(row.Id, cancellationToken))
        {
            return TodaysChange.Conflict;
        }

        row.Sections = changed.ToJson();
        row.Status = redraft ? TodaysMessageStatus.Generating : TodaysMessageStatus.Ready;
        if (!redraft)
        {
            row.Version++;
        }

        ClearApproval(row);
        row.UpdatedAt = now;
        Processed(clientRequestId, SectionEndpoint, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TodaysChange.Applied;
    }

    public async Task<TodaysMessageRow?> GetApprovedAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        await RowAsync(
            db.TodaysMessages.Where(t => t.ClassId == classId && t.Date == date
                && t.Status == TodaysMessageStatus.Approved && t.ApprovedVersion == t.Version),
            cancellationToken);

    public async Task<(string Answer, IReadOnlyList<TodaysAnswerRow> Rows)?> GetAnswersAsync(
        Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        var row = await db.TodaysMessages.AsNoTracking()
            .Where(t => t.ClassId == classId && t.Date == date)
            .Select(t => new { t.Id, t.Sections })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null || TodaysSections.Parse(row.Sections) is not { } sections)
        {
            return null;
        }

        var rows = await (
                from m in db.EveningMessages
                where m.TodaysMessageId == row.Id
                join c in db.Children on m.ChildId equals c.Id
                orderby c.RollNo
                select new TodaysAnswerRow(c.Id, c.FirstName, c.RollNo, m.PictureFirst, m.PictureSecond))
            .ToListAsync(cancellationToken);
        return (sections.Picture.Answer, rows);
    }

    private async Task<TodaysMessageRow?> RowAsync(IQueryable<Data.Entities.TodaysMessage> query, CancellationToken cancellationToken)
    {
        var row = await query
            .Select(t => new
            {
                Row = t,
                Sent = db.EveningMessages.Any(m => m.TodaysMessageId == t.Id),
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new TodaysMessageRow(
                row.Row.Id, row.Row.ClassId, row.Row.Date, row.Row.CompetencyIds, row.Row.Status, row.Row.Version,
                row.Row.ApprovedVersion, row.Row.ApprovedAt, row.Row.Verdict, TodaysSections.Parse(row.Row.Sections),
                row.Row.UpdatedAt, row.Sent);
    }

    private Task<bool> SentAsync(Guid id, CancellationToken cancellationToken) =>
        db.EveningMessages.AnyAsync(m => m.TodaysMessageId == id, cancellationToken);

    private static void ClearApproval(Data.Entities.TodaysMessage row)
    {
        row.ApprovedVersion = null;
        row.ApprovedAt = null;
        row.ApprovedByTeacherId = null;
    }

    private void Processed(Guid clientRequestId, string endpoint, DateTimeOffset now) =>
        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = endpoint, CreatedAt = now });

    private async Task<IDbContextTransaction> LockAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"todays:{classId}:{date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        return transaction;
    }
}
