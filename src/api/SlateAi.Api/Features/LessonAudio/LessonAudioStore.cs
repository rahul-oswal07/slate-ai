using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using Entity = SlateAi.Api.Data.Entities.LessonAudio;

namespace SlateAi.Api.Features.LessonAudio;

public static class LessonAudioStatus
{
    public const string Generating = "generating";
    public const string Ready = "ready";
    public const string Approved = "approved";
    public const string Failed = "failed";
}

public sealed record LessonAudioRow(
    Guid Id,
    Guid ClassId,
    DateOnly Date,
    string Language,
    Guid? ClipId,
    string Status,
    string? Verdict,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset UpdatedAt);

public enum LessonAudioApproval
{
    Approved,

    /// <summary>This clientRequestId was already applied.</summary>
    Replay,

    /// <summary>Not ready, or not the clip she listened to: show her the latest.</summary>
    Stale,

    NotFound,
}

/// <summary>Database side of the optional lesson audio, behind an interface so endpoint tests can use a fake.</summary>
public interface ILessonAudioStore
{
    Task<IReadOnlyList<LessonAudioRow>> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    Task<LessonAudioRow?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a row per language that has none, as generating; a failed row goes back to generating. Returns the ids to
    /// generate (new and restarted rows only).
    /// </summary>
    Task<IReadOnlyList<Guid>> StartAsync(
        Guid classId, DateOnly date, IReadOnlyCollection<string> languages, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Unticked: removes the day's rows (and with them any approval).</summary>
    Task RemoveAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>Try again: back to generating with no clip and no approval. Null if there is no such row.</summary>
    Task<Guid?> RestartAsync(Guid classId, DateOnly date, string language, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Records the outcome of generation, only while the row is still generating (it may have been restarted or removed).</summary>
    Task SetResultAsync(Guid id, Guid? clipId, string status, string? verdict, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Approves exactly <paramref name="clipId"/>, only if the row is ready and that is its current clip.</summary>
    Task<LessonAudioApproval> ApproveAsync(
        Guid classId, DateOnly date, string language, Guid clipId, Guid clientRequestId, Guid teacherId, DateTimeOffset at,
        CancellationToken cancellationToken);
}

public sealed class LessonAudioStore(AppDbContext db) : ILessonAudioStore
{
    private const string ApproveEndpoint = "lesson-audio-approve";

    public async Task<IReadOnlyList<LessonAudioRow>> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        (await db.LessonAudios.Where(l => l.ClassId == classId && l.Date == date).OrderBy(l => l.Language)
            .ToListAsync(cancellationToken)).Select(Row).ToList();

    public async Task<LessonAudioRow?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await db.LessonAudios.Where(l => l.Id == id).Select(l => Row(l)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> StartAsync(
        Guid classId, DateOnly date, IReadOnlyCollection<string> languages, DateTimeOffset at, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(classId, date, cancellationToken);
        var rows = await db.LessonAudios.Where(l => l.ClassId == classId && l.Date == date).ToListAsync(cancellationToken);
        var start = new List<Guid>();
        foreach (var language in languages)
        {
            var row = rows.SingleOrDefault(r => r.Language == language);
            if (row is null)
            {
                row = new Entity
                {
                    Id = Guid.NewGuid(), ClassId = classId, Date = date, Language = language,
                    Status = LessonAudioStatus.Generating, CreatedAt = at, UpdatedAt = at,
                };
                db.LessonAudios.Add(row);
                start.Add(row.Id);
            }
            else if (row.Status == LessonAudioStatus.Failed)
            {
                Reset(row, at);
                start.Add(row.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return start;
    }

    public Task RemoveAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        db.LessonAudios.Where(l => l.ClassId == classId && l.Date == date).ExecuteDeleteAsync(cancellationToken);

    public async Task<Guid?> RestartAsync(
        Guid classId, DateOnly date, string language, DateTimeOffset at, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(classId, date, cancellationToken);
        var row = await db.LessonAudios.SingleOrDefaultAsync(
            l => l.ClassId == classId && l.Date == date && l.Language == language, cancellationToken);
        if (row is null)
        {
            return null;
        }

        Reset(row, at);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return row.Id;
    }

    public Task SetResultAsync(
        Guid id, Guid? clipId, string status, string? verdict, DateTimeOffset at, CancellationToken cancellationToken) =>
        db.LessonAudios.Where(l => l.Id == id && l.Status == LessonAudioStatus.Generating)
            .ExecuteUpdateAsync(
                s => s.SetProperty(l => l.ClipId, clipId).SetProperty(l => l.Status, status)
                    .SetProperty(l => l.Verdict, verdict).SetProperty(l => l.UpdatedAt, at),
                cancellationToken);

    public async Task<LessonAudioApproval> ApproveAsync(
        Guid classId, DateOnly date, string language, Guid clipId, Guid clientRequestId, Guid teacherId, DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync(classId, date, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return LessonAudioApproval.Replay;
        }

        var row = await db.LessonAudios.SingleOrDefaultAsync(
            l => l.ClassId == classId && l.Date == date && l.Language == language, cancellationToken);
        if (row is null)
        {
            return LessonAudioApproval.NotFound;
        }

        // Guardrail 3: only the clip she was shown, and only while it is ready.
        if (row.Status != LessonAudioStatus.Ready || row.ClipId != clipId)
        {
            return LessonAudioApproval.Stale;
        }

        row.Status = LessonAudioStatus.Approved;
        row.ApprovedAt = at;
        row.ApprovedByTeacherId = teacherId;
        row.ApprovedClipId = clipId;
        row.UpdatedAt = at;
        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = ApproveEndpoint, CreatedAt = at });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return LessonAudioApproval.Approved;
    }

    private static void Reset(Entity row, DateTimeOffset at)
    {
        row.Status = LessonAudioStatus.Generating;
        row.ClipId = null;
        row.Verdict = null;
        row.ApprovedAt = null;
        row.ApprovedByTeacherId = null;
        row.ApprovedClipId = null;
        row.UpdatedAt = at;
    }

    private static LessonAudioRow Row(Entity l) =>
        new(l.Id, l.ClassId, l.Date, l.Language, l.ClipId, l.Status, l.Verdict, l.ApprovedAt, l.UpdatedAt);

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> LockAsync(
        Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"lesson-audio:{classId}:{date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        return transaction;
    }
}
