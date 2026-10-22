using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Lessons.Voice;

namespace SlateAi.Api.Features.Lessons;

public static class LessonValues
{
    public const string Picked = "picked";
    public const string Voice = "voice";
    public const string Confirmed = "confirmed";

    // Voice note states before she confirms (T2).
    public const string Processing = "processing";
    public const string Mapped = "mapped";
    public const string Low = "low";
    public const string Unclear = "unclear";
}

/// <summary>A day's lesson as stored.</summary>
public sealed record LessonRecord(
    IReadOnlyList<string> CompetencyIds,
    string Source,
    string Status,
    string? SummaryKn = null,
    string? SummaryEn = null,
    IReadOnlyList<string>? ExamplesUsed = null,
    DateTimeOffset? MappingStartedAt = null);

/// <summary>Names the scrubber removes before anything goes to Claude (never sent anywhere themselves).</summary>
public sealed record ClassNames(IReadOnlyList<string> Children, string? Teacher);

public enum VoiceStart
{
    /// <summary>Queued for matching.</summary>
    Started,

    /// <summary>This clientRequestId was already applied (outbox replay): nothing to do.</summary>
    Replay,

    /// <summary>The day's lesson is already confirmed (D-55: no second note; she uses Change).</summary>
    AlreadyConfirmed,
}

/// <summary>Database side of lessons, kept behind an interface so endpoint tests can use a fake.</summary>
public interface ILessonStore
{
    Task<LessonRecord?> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>Competencies of the latest confirmed lesson before <paramref name="date"/> (empty if none).</summary>
    Task<IReadOnlyList<string>> LastConfirmedBeforeAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or replaces the day's lesson as confirmed (D-55: one per day; "Change" overwrites), unless
    /// <paramref name="clientRequestId"/> was already applied. Returns the stored lesson.
    /// </summary>
    Task<LessonRecord> SaveConfirmedAsync(
        Guid classId,
        Guid clientRequestId,
        DateOnly date,
        IReadOnlyList<string> competencyIds,
        string source,
        CancellationToken cancellationToken);

    /// <summary>
    /// A voice note arrived: marks the day's lesson as processing (replacing an unconfirmed one) and records
    /// <paramref name="startedAt"/>, which identifies this note's result later.
    /// </summary>
    Task<VoiceStart> StartVoiceAsync(
        Guid classId, Guid clientRequestId, DateOnly date, DateTimeOffset startedAt, CancellationToken cancellationToken);

    /// <summary>
    /// Stores A1's result, but only if the lesson is still processing the note started at
    /// <paramref name="startedAt"/> (she may have picked from the list meanwhile).
    /// </summary>
    Task SaveMappingAsync(
        Guid classId, DateOnly date, DateTimeOffset startedAt, A1Result result, CancellationToken cancellationToken);

    Task<ClassNames> GetNamesAsync(Guid classId, CancellationToken cancellationToken);
}

public sealed class LessonStore(AppDbContext db, TimeProvider clock) : ILessonStore
{
    private const string ConfirmEndpoint = "lesson";
    private const string AudioEndpoint = "lesson-audio";

    public Task<LessonRecord?> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        db.Lessons
            .Where(l => l.ClassId == classId && l.Date == date)
            .Select(l => new LessonRecord(
                l.CompetencyIds, l.Source, l.Status, l.SummaryKn, l.SummaryEn, l.ExamplesUsed, l.MappingStartedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> LastConfirmedBeforeAsync(
        Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        await db.Lessons
            .Where(l => l.ClassId == classId && l.Date < date && l.Status == LessonValues.Confirmed)
            .OrderByDescending(l => l.Date)
            .Select(l => l.CompetencyIds)
            .FirstOrDefaultAsync(cancellationToken) ?? [];

    public async Task<LessonRecord> SaveConfirmedAsync(
        Guid classId,
        Guid clientRequestId,
        DateOnly date,
        IReadOnlyList<string> competencyIds,
        string source,
        CancellationToken cancellationToken)
    {
        await using (var transaction = await LockDayAsync(classId, date, cancellationToken))
        {
            if (!await IsProcessedAsync(clientRequestId, cancellationToken))
            {
                var lesson = await FindOrAddAsync(classId, date, source, cancellationToken);
                lesson.CompetencyIds = [.. competencyIds];
                lesson.Source = source;
                lesson.Status = LessonValues.Confirmed;
                MarkProcessed(clientRequestId, ConfirmEndpoint);
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return (await GetDayAsync(classId, date, cancellationToken))!;
    }

    public async Task<VoiceStart> StartVoiceAsync(
        Guid classId, Guid clientRequestId, DateOnly date, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        await using var transaction = await LockDayAsync(classId, date, cancellationToken);
        if (await IsProcessedAsync(clientRequestId, cancellationToken))
        {
            return VoiceStart.Replay;
        }

        var lesson = await FindOrAddAsync(classId, date, LessonValues.Voice, cancellationToken);
        if (lesson.Status == LessonValues.Confirmed)
        {
            return VoiceStart.AlreadyConfirmed;
        }

        lesson.Source = LessonValues.Voice;
        lesson.Status = LessonValues.Processing;
        lesson.CompetencyIds = [];
        lesson.SummaryKn = null;
        lesson.SummaryEn = null;
        lesson.ExamplesUsed = [];
        lesson.Confidence = null;
        lesson.MappingStartedAt = startedAt.ToUniversalTime();
        MarkProcessed(clientRequestId, AudioEndpoint);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return VoiceStart.Started;
    }

    public async Task SaveMappingAsync(
        Guid classId, DateOnly date, DateTimeOffset startedAt, A1Result result, CancellationToken cancellationToken)
    {
        var started = startedAt.ToUniversalTime();
        await db.Lessons
            .Where(l => l.ClassId == classId && l.Date == date
                && l.Status == LessonValues.Processing && l.MappingStartedAt == started)
            .ExecuteUpdateAsync(
                s => s.SetProperty(l => l.Status, result.Status)
                    .SetProperty(l => l.CompetencyIds, result.CompetencyIds.ToList())
                    .SetProperty(l => l.SummaryKn, result.SummaryKn)
                    .SetProperty(l => l.SummaryEn, result.SummaryEn)
                    .SetProperty(l => l.ExamplesUsed, result.ExamplesUsed.ToList())
                    .SetProperty(l => l.Confidence, result.Confidence),
                cancellationToken);
    }

    public async Task<ClassNames> GetNamesAsync(Guid classId, CancellationToken cancellationToken)
    {
        var children = await db.Children.Where(c => c.ClassId == classId).Select(c => c.FirstName).ToListAsync(cancellationToken);
        var teacher = await db.Classes
            .Where(c => c.Id == classId)
            .Join(db.Teachers, c => c.TeacherId, t => t.Id, (_, t) => t.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);
        return new ClassNames(children, teacher);
    }

    /// <summary>Serialises writes per class and date (replays, a pick racing a voice note).</summary>
    private async Task<IDbContextTransaction> LockDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = $"lesson:{classId}:{date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);
        return transaction;
    }

    private Task<bool> IsProcessedAsync(Guid clientRequestId, CancellationToken cancellationToken) =>
        db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken);

    private void MarkProcessed(Guid clientRequestId, string endpoint) =>
        db.ProcessedRequests.Add(new ProcessedRequest
        {
            ClientRequestId = clientRequestId,
            Endpoint = endpoint,
            CreatedAt = clock.GetUtcNow(),
        });

    private async Task<Lesson> FindOrAddAsync(Guid classId, DateOnly date, string source, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons.SingleOrDefaultAsync(l => l.ClassId == classId && l.Date == date, cancellationToken);
        if (lesson is null)
        {
            lesson = new Lesson { Id = Guid.NewGuid(), ClassId = classId, Date = date, Source = source, Status = LessonValues.Processing };
            db.Lessons.Add(lesson);
        }

        return lesson;
    }
}
