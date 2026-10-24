using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Features.Demo;

/// <summary>One message on the simulated parent phone: an evening link or a catch-up link.</summary>
public sealed record FamilyMessage(string Kind, DateOnly Date, DateTimeOffset SentAt, string Token)
{
    public const string Evening = "evening";
    public const string CatchUp = "catchUp";
}

/// <summary>One row of the "Under the hood" panel. <see cref="AiCall"/> holds no prompt text or names.</summary>
public sealed record AiCallRow(
    Guid Id, DateTimeOffset CreatedAt, string Purpose, string Model, int? PromptVersion,
    int InputTokens, int OutputTokens, int CacheReadTokens, int LatencyMs, string? Verdict, string? Error);

/// <summary>
/// What Reset writes besides the seed class: the story's past days and, when there is one, the pre-made catch-up for
/// <see cref="SnapshotChildId"/> with what they missed so far (<see cref="SnapshotMissed"/>, or the snapshot's own).
/// </summary>
public sealed record DemoReset(
    Guid ClientRequestId,
    DemoSeedFile Seed,
    IReadOnlyList<DemoDay> Days,
    TimeOnly RegisterArrivesAt,
    DateOnly WeekOf,
    DemoSnapshot? Snapshot,
    Guid? SnapshotChildId,
    IReadOnlyList<string> SnapshotMissed);

public enum DemoResetResult
{
    Reset,
    Replay,
}

/// <summary>Database side of the demo shell (T-18), behind an interface so endpoint tests use a fake.</summary>
public interface IDemoStore
{
    /// <summary>
    /// Puts the demo class back to the start of the story in one transaction: deletes everything the class has done
    /// (children re-added from the seed, so consent, stops and deletions are undone), then writes the story's days and the
    /// pre-made catch-up. A replayed clientRequestId changes nothing. AI call rows stay: they are the cost log.
    /// </summary>
    Task<DemoResetResult> ResetAsync(DemoReset reset, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>The links sent to a child's family, newest first; null when the child isn't in the class.</summary>
    Task<IReadOnlyList<FamilyMessage>?> GetFamilyAsync(Guid classId, Guid childId, CancellationToken cancellationToken);

    /// <summary>When the demo was last reset (the Reset's own request record), or null if never.</summary>
    Task<DateTimeOffset?> GetLastResetAsync(CancellationToken cancellationToken);

    /// <summary>The latest AI calls, newest first, optionally only those from <paramref name="since"/>.</summary>
    Task<IReadOnlyList<AiCallRow>> GetAiCallsAsync(DateTimeOffset? since, int limit, CancellationToken cancellationToken);

    /// <summary>A package of the class with its audio, to save as the demo's pre-made catch-up; null if not found.</summary>
    Task<DemoSnapshot?> ExportAsync(Guid classId, Guid packageId, CancellationToken cancellationToken);
}

public sealed class DemoStore(AppDbContext db) : IDemoStore
{
    private const string ResetEndpoint = "demo-reset";

    public async Task<DemoResetResult> ResetAsync(DemoReset reset, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var classId = reset.Seed.Class.Id;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({DemoSeeder.SeedLockKey})", cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == reset.ClientRequestId, cancellationToken))
        {
            return DemoResetResult.Replay;
        }

        // Children first: their rows cascade (attendance, evening messages, catch-up links and answers, reasons, buddy
        // cards). Packages cascade their own audio; shared clips (the disclosure) have no package and stay.
        await db.PacingDecisions.Where(d => d.ClassId == classId).ExecuteDeleteAsync(cancellationToken);
        await db.Children.Where(c => c.ClassId == classId).ExecuteDeleteAsync(cancellationToken);
        await db.CatchUpPackages.Where(p => p.ClassId == classId).ExecuteDeleteAsync(cancellationToken);
        await db.Attendance.Where(a => a.ClassId == classId).ExecuteDeleteAsync(cancellationToken);
        await db.Lessons.Where(l => l.ClassId == classId).ExecuteDeleteAsync(cancellationToken);

        db.Children.AddRange(reset.Seed.Children.Select(c => c.ToEntity(classId)));
        foreach (var day in reset.Days)
        {
            db.Lessons.Add(new Lesson
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                Date = day.Date,
                Source = LessonValues.Picked,
                CompetencyIds = [day.CompetencyId],
                Status = LessonValues.Confirmed,
            });
            db.Attendance.AddRange(reset.Seed.Children.Select(c => new Data.Entities.Attendance
            {
                ClassId = classId,
                Date = day.Date,
                ChildId = c.Id,
                Present = !day.AbsentChildIds.Contains(c.Id),
                Source = AttendanceSources.Sats,
                SourcePresent = !day.AbsentChildIds.Contains(c.Id),
                ReceivedAt = SchoolClock.At(day.Date, reset.RegisterArrivesAt).ToUniversalTime(),
            }));
        }

        if (reset is { Snapshot: { } snapshot, SnapshotChildId: { } childId })
        {
            await AddSnapshotAsync(reset, snapshot, childId, now, cancellationToken);
        }

        db.ProcessedRequests.Add(new ProcessedRequest
        {
            ClientRequestId = reset.ClientRequestId,
            Endpoint = ResetEndpoint,
            CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return DemoResetResult.Reset;
    }

    public async Task<IReadOnlyList<FamilyMessage>?> GetFamilyAsync(
        Guid classId, Guid childId, CancellationToken cancellationToken)
    {
        if (!await db.Children.AnyAsync(c => c.Id == childId && c.ClassId == classId, cancellationToken))
        {
            return null;
        }

        var evening = await db.EveningMessages
            .Where(m => m.ChildId == childId)
            .Select(m => new FamilyMessage(FamilyMessage.Evening, m.Date, m.SentAt, m.Token))
            .ToListAsync(cancellationToken);
        var catchUps = await db.CatchUpAssignments
            .Where(a => a.ChildId == childId && a.Token != null && a.SentAt != null)
            .Select(a => new FamilyMessage(FamilyMessage.CatchUp, a.WeekOf, a.SentAt!.Value, a.Token!))
            .ToListAsync(cancellationToken);
        return evening.Concat(catchUps).OrderByDescending(m => m.SentAt).ToList();
    }

    public Task<DateTimeOffset?> GetLastResetAsync(CancellationToken cancellationToken) =>
        db.ProcessedRequests
            .Where(r => r.Endpoint == ResetEndpoint)
            .MaxAsync(r => (DateTimeOffset?)r.CreatedAt, cancellationToken);

    public async Task<IReadOnlyList<AiCallRow>> GetAiCallsAsync(
        DateTimeOffset? since, int limit, CancellationToken cancellationToken) =>
        await db.AiCalls
            .Where(c => since == null || c.CreatedAt >= since)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new AiCallRow(
                c.Id, c.CreatedAt, c.Purpose, c.Model, c.PromptVersion,
                c.InputTokens, c.OutputTokens, c.CacheReadTokens, c.LatencyMs, c.Verdict, c.Error))
            .ToListAsync(cancellationToken);

    public async Task<DemoSnapshot?> ExportAsync(Guid classId, Guid packageId, CancellationToken cancellationToken)
    {
        var package = await db.CatchUpPackages.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == packageId && p.ClassId == classId, cancellationToken);
        if (package is null)
        {
            return null;
        }

        var assignment = await db.CatchUpAssignments.AsNoTracking()
            .Where(a => a.PackageId == packageId)
            .OrderBy(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var clipIds = DemoSnapshot.ClipIds(package);
        var clips = await db.AudioClips.AsNoTracking().Where(c => clipIds.Contains(c.Id)).ToListAsync(cancellationToken);
        return DemoSnapshot.From(package, assignment, clips);
    }

    private async Task AddSnapshotAsync(
        DemoReset reset, DemoSnapshot snapshot, Guid childId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var packageId = Guid.NewGuid();
        var newIds = new Dictionary<Guid, Guid>();
        foreach (var clip in snapshot.Clips)
        {
            // A shared clip (the disclosure) may already be stored under another ID: reuse it.
            var existing = clip.TextHash is null
                ? null
                : await db.AudioClips
                    .Where(c => c.Kind == clip.Kind && c.Language == clip.Language && c.TextHash == clip.TextHash)
                    .Select(c => (Guid?)c.Id)
                    .SingleOrDefaultAsync(cancellationToken);
            if (existing is { } id)
            {
                newIds[clip.Id] = id;
                continue;
            }

            var clipId = Guid.NewGuid();
            newIds[clip.Id] = clipId;
            db.AudioClips.Add(new AudioClip
            {
                Id = clipId,
                Kind = clip.Kind,
                Language = clip.Language,
                PackageId = clip.Shared ? null : packageId,
                TextHash = clip.TextHash,
                ContentType = clip.ContentType,
                Bytes = Convert.FromBase64String(clip.Base64),
                Seconds = clip.Seconds,
                CreatedAt = now,
            });
        }

        db.CatchUpPackages.Add(new CatchUpPackage
        {
            Id = packageId,
            ClassId = reset.Seed.Class.Id,
            WeekOf = reset.WeekOf,
            TargetCompetencyIds = snapshot.TargetCompetencyIds.ToList(),
            TargetKey = snapshot.TargetKey,
            Language = snapshot.Language,
            Status = PackageStatus.Ready,
            ScriptVersion = 1,
            PromptVersion = snapshot.PromptVersion,
            Verdict = snapshot.Verdict,
            Script = snapshot.Script,
            KeyWordsKn = snapshot.KeyWordsKn.ToList(),
            GlossKn = snapshot.GlossKn,
            GlossEn = snapshot.GlossEn,
            Activity = snapshot.Activity,
            CheckItem = snapshot.CheckItem,
            Review = snapshot.Review,
            AudioUrl = DemoSnapshot.Remap(snapshot.AudioUrl, newIds),
            AudioSegments = DemoSnapshot.Remap(snapshot.AudioSegments, newIds),
            CreatedAt = now,
            UpdatedAt = now,
        });
        db.CatchUpAssignments.Add(new CatchUpAssignment
        {
            Id = Guid.NewGuid(),
            ClassId = reset.Seed.Class.Id,
            ChildId = childId,
            PackageId = packageId,
            WeekOf = reset.WeekOf,
            MissedCompetencyIds = (reset.SnapshotMissed.Count > 0 ? reset.SnapshotMissed : snapshot.MissedCompetencyIds).ToList(),
            CompressionRationale = snapshot.CompressionRationale,
            CreatedAt = now,
        });
    }
}
