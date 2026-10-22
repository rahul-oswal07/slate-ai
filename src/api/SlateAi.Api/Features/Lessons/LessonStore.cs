using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Lessons;

public static class LessonValues
{
    public const string Picked = "picked";
    public const string Voice = "voice";
    public const string Confirmed = "confirmed";
}

/// <summary>A day's lesson as stored.</summary>
public sealed record LessonRecord(IReadOnlyList<string> CompetencyIds, string Source, string Status);

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
}

public sealed class LessonStore(AppDbContext db, TimeProvider clock) : ILessonStore
{
    private const string EndpointName = "lesson";

    public Task<LessonRecord?> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        db.Lessons
            .Where(l => l.ClassId == classId && l.Date == date)
            .Select(l => new LessonRecord(l.CompetencyIds, l.Source, l.Status))
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
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = $"lesson:{classId}:{date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);

        if (!await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            var lesson = await db.Lessons.SingleOrDefaultAsync(l => l.ClassId == classId && l.Date == date, cancellationToken);
            if (lesson is null)
            {
                lesson = new Lesson
                {
                    Id = Guid.NewGuid(),
                    ClassId = classId,
                    Date = date,
                    Source = source,
                    Status = LessonValues.Confirmed,
                };
                db.Lessons.Add(lesson);
            }

            lesson.CompetencyIds = [.. competencyIds];
            lesson.Source = source;
            lesson.Status = LessonValues.Confirmed;
            db.ProcessedRequests.Add(new ProcessedRequest
            {
                ClientRequestId = clientRequestId,
                Endpoint = EndpointName,
                CreatedAt = clock.GetUtcNow(),
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (await GetDayAsync(classId, date, cancellationToken))!;
    }
}
