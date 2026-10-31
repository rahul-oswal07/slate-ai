using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Features.CatchUps;

public static class PackageStatus
{
    public const string Generating = "generating";
    public const string Ready = "ready";
    public const string NeedsReview = "needsReview";
    public const string Approved = "approved";
    public const string Sent = "sent";
    public const string Failed = "failed";
}

/// <summary>The activity as stored: code-made numbers plus A3's words (P2).</summary>
public sealed record ActivityData(
    string CompetencyId, string Kind, string Question, IReadOnlyList<int> Numbers, int Answer, string? Instruction, string? ObjectOption);

/// <summary>The learning check as stored (P3). <see cref="CorrectIndex"/> never goes to a parent's browser.</summary>
public sealed record CheckData(
    string CompetencyId, string Kind, string Question, IReadOnlyList<int> Options, int CorrectIndex, int Answer, string? Prompt);

/// <param name="ChildIds">The children it is assigned to: the AI calls that write it are about them (T-73).</param>
public sealed record PackageRecord(
    Guid Id, Guid ClassId, DateOnly WeekOf, IReadOnlyList<string> TargetIds, string TargetKey, string Language,
    string Status, int ScriptVersion, string? Hint, IReadOnlyList<Guid>? ChildIds = null);

/// <summary>The verifier's review of the stored attempt (A4); null before verification.</summary>
public sealed record PackageReview(bool Passed, int Attempts, IReadOnlyList<VerifierCheck> Checks);

/// <summary>What writing a catch-up ended with.</summary>
/// <param name="Status">ready, needsReview or failed.</param>
/// <param name="Content">The words to store; null when nothing usable came back (failed).</param>
/// <param name="Audio">The audio segments; null when no audio was made.</param>
public sealed record ScriptOutcome(
    string Status, ScriptContent? Content, string Verdict, int PromptVersion, PackageReview? Review, AudioSegments? Audio = null);

/// <param name="FirstName">For the teacher's screen only; never sent to an AI provider.</param>
/// <param name="Sent">This family got the link.</param>
/// <param name="Reachable">The family would get it: consent "yes" and not stopped (others: buddy card only).</param>
public sealed record AssignedChild(
    Guid ChildId, IReadOnlyList<string> MissedCompetencyIds, CompressionRationale Rationale, string FirstName = "",
    bool Sent = false, bool Reachable = false);

/// <summary>What an approve, regenerate or send asked of the store found.</summary>
public enum PackageChange
{
    Applied,

    /// <summary>This clientRequestId was already applied.</summary>
    Replay,

    /// <summary>Not allowed in the package's current state or version: show the latest (409).</summary>
    Conflict,

    NotFound,
}

/// <summary>A catch-up as the teacher's Approve screen will read it (T3).</summary>
public sealed record PackageView(
    Guid Id,
    string Language,
    IReadOnlyList<string> TargetCompetencyIds,
    string Status,
    int ScriptVersion,
    string? Verdict,
    string? Script,
    IReadOnlyList<string> KeyWordsKn,
    string? GlossKn,
    string? GlossEn,
    ActivityData Activity,
    CheckData Check,
    IReadOnlyList<AssignedChild> Children,
    DateTimeOffset UpdatedAt,
    PackageReview? Review,
    AudioSegments? Audio,
    Guid ClassId,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? SentAt)
{
    /// <summary>Families the link went to (counted in code).</summary>
    public int FamiliesSent => Children.Count(c => c.Sent);

    /// <summary>Families it goes to once sent: consent yes, not stopped (counted in code).</summary>
    public int FamiliesReachable => Children.Count(c => c.Reachable);
}

/// <summary>Database side of catch-ups, behind an interface so endpoint tests can use a fake.</summary>
public interface ICatchUpStore
{
    Task<IReadOnlySet<Guid>> GetAssignedChildIdsAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken);

    /// <summary>Each child's home language, or null if the class doesn't exist.</summary>
    Task<IReadOnlyDictionary<Guid, string>?> GetChildLanguagesAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>The class's child and teacher names, only to refuse A3 output that contains one.</summary>
    Task<IReadOnlyList<string>> GetNamesAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>Examples she used in the week's confirmed lessons on these competencies (A1, name-scrubbed).</summary>
    Task<IReadOnlyList<string>> GetTeacherExamplesAsync(
        Guid classId, DateOnly from, DateOnly to, IReadOnlyList<string> competencyIds, CancellationToken cancellationToken);

    /// <summary>
    /// Adds the week's missing packages (as generating, or failed without numbers) and missing assignments. Existing
    /// ones are kept. Returns the IDs of new packages that need words.
    /// </summary>
    Task<IReadOnlyList<Guid>> SaveGenerationAsync(
        Guid classId, DateOnly weekOf, IReadOnlyList<PlannedPackage> packages, IReadOnlyList<PlannedAssignment> assignments,
        CancellationToken cancellationToken);

    Task<PackageRecord?> GetPackageAsync(Guid packageId, CancellationToken cancellationToken);

    /// <summary>Stores the outcome (words, status, review) with a new script version.</summary>
    Task SaveScriptAsync(Guid packageId, ScriptOutcome outcome, CancellationToken cancellationToken);

    Task<IReadOnlyList<PackageView>> ListAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken);

    Task<PackageView?> GetViewAsync(Guid packageId, CancellationToken cancellationToken);

    /// <summary>Approves only a ready package at <paramref name="scriptVersion"/> (T3: approval bound to the version she ticked).</summary>
    Task<PackageChange> ApproveAsync(
        Guid packageId, Guid clientRequestId, int scriptVersion, Guid teacherId, CancellationToken cancellationToken);

    /// <summary>Sets a ready, needs-review or failed package back to generating with the hint (Simpler / Shorter / Try again).</summary>
    Task<PackageChange> StartRegenerateAsync(Guid packageId, Guid clientRequestId, string? hint, CancellationToken cancellationToken);

    /// <summary>
    /// Guardrail 3: only an approved package at its approved version is sent. Each assignment whose family has consent
    /// "yes" and hasn't stopped gets a link token; the package becomes sent. Repeating adds nothing.
    /// </summary>
    Task<PackageChange> SendAsync(
        Guid packageId, Func<string> newToken, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    /// <summary>Approved catch-ups not sent yet, for the 6 pm job.</summary>
    Task<IReadOnlyList<Guid>> GetApprovedUnsentAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken);
}

public sealed class CatchUpStore(AppDbContext db, TimeProvider clock) : ICatchUpStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlySet<Guid>> GetAssignedChildIdsAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken) =>
        (await db.CatchUpAssignments.Where(a => a.ClassId == classId && a.WeekOf == weekOf).Select(a => a.ChildId)
            .ToListAsync(cancellationToken)).ToHashSet();

    public async Task<IReadOnlyDictionary<Guid, string>?> GetChildLanguagesAsync(Guid classId, CancellationToken cancellationToken)
    {
        if (!await db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            return null;
        }

        return await db.Children.Where(c => c.ClassId == classId)
            .ToDictionaryAsync(c => c.Id, c => c.HomeLanguage, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetNamesAsync(Guid classId, CancellationToken cancellationToken)
    {
        var names = await db.Children.Where(c => c.ClassId == classId).Select(c => c.FirstName).ToListAsync(cancellationToken);
        var teacher = await db.Classes.Where(c => c.Id == classId)
            .Join(db.Teachers, c => c.TeacherId, t => t.Id, (_, t) => t.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);
        return teacher is null ? names : [.. names, teacher];
    }

    public async Task<IReadOnlyList<string>> GetTeacherExamplesAsync(
        Guid classId, DateOnly from, DateOnly to, IReadOnlyList<string> competencyIds, CancellationToken cancellationToken)
    {
        var lessons = await db.Lessons
            .Where(l => l.ClassId == classId && l.Date >= from && l.Date <= to && l.Status == LessonValues.Confirmed)
            .Select(l => new { l.CompetencyIds, l.ExamplesUsed })
            .ToListAsync(cancellationToken);
        return lessons.Where(l => l.CompetencyIds.Intersect(competencyIds).Any())
            .SelectMany(l => l.ExamplesUsed).Distinct().Take(5).ToList();
    }

    public async Task<IReadOnlyList<Guid>> SaveGenerationAsync(
        Guid classId, DateOnly weekOf, IReadOnlyList<PlannedPackage> packages, IReadOnlyList<PlannedAssignment> assignments,
        CancellationToken cancellationToken)
    {
        // Two generates at once (double tap, two devices) must not create duplicates.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = $"catchups:{classId}:{weekOf:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);

        var now = clock.GetUtcNow();
        var stored = await db.CatchUpPackages.Where(p => p.ClassId == classId && p.WeekOf == weekOf).ToListAsync(cancellationToken);
        var created = new List<Guid>();
        foreach (var planned in packages)
        {
            if (stored.Any(p => p.Language == planned.Language && p.TargetKey == planned.TargetKey))
            {
                continue;
            }

            var package = NewPackage(classId, weekOf, planned, now);
            db.CatchUpPackages.Add(package);
            stored.Add(package);
            if (package.Status == PackageStatus.Generating)
            {
                created.Add(package.Id);
            }
        }

        var assigned = (await db.CatchUpAssignments.Where(a => a.ClassId == classId && a.WeekOf == weekOf)
            .Select(a => a.ChildId).ToListAsync(cancellationToken)).ToHashSet();
        foreach (var planned in assignments.Where(a => !assigned.Contains(a.ChildId)))
        {
            db.CatchUpAssignments.Add(new CatchUpAssignment
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                ChildId = planned.ChildId,
                PackageId = stored.Single(p => p.Language == planned.Language && p.TargetKey == planned.TargetKey).Id,
                WeekOf = weekOf,
                MissedCompetencyIds = [.. planned.MissedCompetencyIds],
                CompressionRationale = planned.RationaleJson,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    public Task<PackageRecord?> GetPackageAsync(Guid packageId, CancellationToken cancellationToken) =>
        db.CatchUpPackages.Where(p => p.Id == packageId)
            .Select(p => new PackageRecord(
                p.Id, p.ClassId, p.WeekOf, p.TargetCompetencyIds, p.TargetKey, p.Language, p.Status, p.ScriptVersion, p.Hint,
                db.CatchUpAssignments.Where(a => a.PackageId == p.Id).OrderBy(a => a.CreatedAt).Select(a => a.ChildId).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SaveScriptAsync(Guid packageId, ScriptOutcome outcome, CancellationToken cancellationToken)
    {
        var package = await db.CatchUpPackages.SingleAsync(p => p.Id == packageId, cancellationToken);
        package.ScriptVersion++;
        package.PromptVersion = outcome.PromptVersion;
        package.Verdict = outcome.Verdict;
        package.Status = outcome.Status;
        package.Review = outcome.Review is null ? null : JsonSerializer.Serialize(outcome.Review, Json);
        package.AudioSegments = outcome.Audio is null ? null : JsonSerializer.Serialize(outcome.Audio, Json);
        package.AudioUrl = outcome.Audio?.Segments.LastOrDefault(s => s.Kind == AudioKind.Script)?.Url;
        package.UpdatedAt = clock.GetUtcNow();
        if (outcome.Content is { } content)
        {
            package.Script = content.Script;
            package.KeyWordsKn = [.. content.KeyWordsKn];
            package.GlossKn = content.GlossKn;
            package.GlossEn = content.GlossEn;
            var activity = JsonSerializer.Deserialize<ActivityData>(package.Activity, Json)!;
            package.Activity = JsonSerializer.Serialize(
                activity with { Instruction = content.ActivityInstruction, ObjectOption = content.ObjectOption }, Json);
            var check = JsonSerializer.Deserialize<CheckData>(package.CheckItem, Json)!;
            package.CheckItem = JsonSerializer.Serialize(check with { Prompt = content.CheckPrompt }, Json);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PackageView>> ListAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken) =>
        ViewsAsync(db.CatchUpPackages.Where(p => p.ClassId == classId && p.WeekOf == weekOf), cancellationToken);

    public async Task<PackageView?> GetViewAsync(Guid packageId, CancellationToken cancellationToken) =>
        (await ViewsAsync(db.CatchUpPackages.Where(p => p.Id == packageId), cancellationToken)).SingleOrDefault();

    public async Task<PackageChange> ApproveAsync(
        Guid packageId, Guid clientRequestId, int scriptVersion, Guid teacherId, CancellationToken cancellationToken)
    {
        await using var transaction = await LockPackageAsync(packageId, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return PackageChange.Replay;
        }

        if (await db.CatchUpPackages.SingleOrDefaultAsync(p => p.Id == packageId, cancellationToken) is not { } package)
        {
            return PackageChange.NotFound;
        }

        if (package.Status != PackageStatus.Ready || package.ScriptVersion != scriptVersion)
        {
            return PackageChange.Conflict;
        }

        var now = clock.GetUtcNow();
        package.Status = PackageStatus.Approved;
        package.ApprovedAt = now;
        package.ApprovedByTeacherId = teacherId;
        package.ApprovedScriptVersion = scriptVersion;
        package.UpdatedAt = now;
        Processed(clientRequestId, "package-approve", now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return PackageChange.Applied;
    }

    public async Task<PackageChange> StartRegenerateAsync(
        Guid packageId, Guid clientRequestId, string? hint, CancellationToken cancellationToken)
    {
        await using var transaction = await LockPackageAsync(packageId, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return PackageChange.Replay;
        }

        if (await db.CatchUpPackages.SingleOrDefaultAsync(p => p.Id == packageId, cancellationToken) is not { } package)
        {
            return PackageChange.NotFound;
        }

        if (package.Status is not (PackageStatus.Ready or PackageStatus.NeedsReview or PackageStatus.Failed))
        {
            return PackageChange.Conflict;
        }

        var now = clock.GetUtcNow();
        package.Status = PackageStatus.Generating;
        package.Hint = hint;
        package.UpdatedAt = now;
        Processed(clientRequestId, "package-regenerate", now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return PackageChange.Applied;
    }

    public async Task<PackageChange> SendAsync(
        Guid packageId, Func<string> newToken, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using var transaction = await LockPackageAsync(packageId, cancellationToken);
        if (await db.CatchUpPackages.SingleOrDefaultAsync(p => p.Id == packageId, cancellationToken) is not { } package)
        {
            return PackageChange.NotFound;
        }

        if (package.Status == PackageStatus.Sent)
        {
            return PackageChange.Replay;
        }

        if (!CatchUpSendRules.CanSend(package.Status, package.ApprovedScriptVersion, package.ScriptVersion))
        {
            return PackageChange.Conflict;
        }

        var families = await (
                from a in db.CatchUpAssignments
                where a.PackageId == packageId && a.Token == null
                join c in db.Children on a.ChildId equals c.Id
                select new { Assignment = a, c.Consent, Stopped = c.ParentStoppedAt != null })
            .ToListAsync(cancellationToken);
        foreach (var family in families.Where(f => CatchUpSendRules.ReachesFamily(f.Consent, f.Stopped)))
        {
            family.Assignment.Token = newToken();
            family.Assignment.ScheduledAt = now;
            family.Assignment.SentAt = now;
            family.Assignment.ExpiresAt = expiresAt;
        }

        package.Status = PackageStatus.Sent;
        package.SentAt = now;
        package.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return PackageChange.Applied;
    }

    public async Task<IReadOnlyList<Guid>> GetApprovedUnsentAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken) =>
        await db.CatchUpPackages
            .Where(p => p.ClassId == classId && p.WeekOf == weekOf && p.Status == PackageStatus.Approved)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<PackageView>> ViewsAsync(IQueryable<CatchUpPackage> query, CancellationToken cancellationToken)
    {
        // Untracked: an endpoint reads the view before approving; a tracked copy would hide the state another
        // request committed while this one waited for the package lock.
        var packages = await query.AsNoTracking().OrderBy(p => p.Language).ThenBy(p => p.TargetKey).ToListAsync(cancellationToken);
        var ids = packages.Select(p => p.Id).ToList();
        var assignments = await (
                from a in db.CatchUpAssignments.AsNoTracking()
                where ids.Contains(a.PackageId)
                join c in db.Children on a.ChildId equals c.Id
                orderby c.RollNo
                select new { Assignment = a, c.FirstName, c.Consent, Stopped = c.ParentStoppedAt != null })
            .ToListAsync(cancellationToken);
        return packages.Select(p => new PackageView(
                p.Id, p.Language, p.TargetCompetencyIds, p.Status, p.ScriptVersion, p.Verdict, p.Script, p.KeyWordsKn,
                p.GlossKn, p.GlossEn,
                JsonSerializer.Deserialize<ActivityData>(p.Activity, Json)!,
                JsonSerializer.Deserialize<CheckData>(p.CheckItem, Json)!,
                assignments.Where(a => a.Assignment.PackageId == p.Id)
                    .Select(a => new AssignedChild(
                        a.Assignment.ChildId, a.Assignment.MissedCompetencyIds,
                        JsonSerializer.Deserialize<CompressionRationale>(a.Assignment.CompressionRationale, Json)!,
                        a.FirstName, a.Assignment.Token != null, CatchUpSendRules.ReachesFamily(a.Consent, a.Stopped)))
                    .ToList(),
                p.UpdatedAt,
                p.Review is null ? null : JsonSerializer.Deserialize<PackageReview>(p.Review, Json),
                p.AudioSegments is null ? null : JsonSerializer.Deserialize<AudioSegments>(p.AudioSegments, Json),
                p.ClassId,
                p.ApprovedAt,
                p.SentAt))
            .ToList();
    }

    /// <summary>Serialises approve, regenerate, send and the writer's save for one package.</summary>
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> LockPackageAsync(
        Guid packageId, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"package:{packageId}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        return transaction;
    }

    private void Processed(Guid clientRequestId, string endpoint, DateTimeOffset now) =>
        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = endpoint, CreatedAt = now });

    /// <summary>A new package with its numbers; without a practice rule it can't be written, so it starts as failed.</summary>
    public static CatchUpPackage NewPackage(Guid classId, DateOnly weekOf, PlannedPackage planned, DateTimeOffset now)
    {
        var problems = planned.Problems;
        return new CatchUpPackage
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            WeekOf = weekOf,
            TargetCompetencyIds = [.. planned.TargetIds],
            TargetKey = planned.TargetKey,
            Language = planned.Language,
            Status = problems is null ? PackageStatus.Failed : PackageStatus.Generating,
            Verdict = problems is null ? "fallback:no-practice" : null,
            Activity = JsonSerializer.Serialize(problems is null
                ? new ActivityData(planned.TargetIds[0], "", "", [], 0, null, null)
                : new ActivityData(problems.CompetencyId, problems.Activity.Kind, problems.Activity.Question,
                    problems.Activity.Numbers, problems.Activity.Answer, null, null), Json),
            CheckItem = JsonSerializer.Serialize(problems is null
                ? new CheckData(planned.TargetIds[0], "", "", [], 0, 0, null)
                : new CheckData(problems.CompetencyId, problems.Check.Kind, problems.Check.Question, problems.Check.Options,
                    problems.Check.CorrectIndex, problems.Check.Answer, null), Json),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
