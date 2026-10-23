using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
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

public sealed record PackageRecord(
    Guid Id, Guid ClassId, DateOnly WeekOf, IReadOnlyList<string> TargetIds, string TargetKey, string Language,
    string Status, int ScriptVersion, string? Hint);

/// <summary>The verifier's review of the stored attempt (A4); null before verification.</summary>
public sealed record PackageReview(bool Passed, int Attempts, IReadOnlyList<VerifierCheck> Checks);

/// <summary>What writing a catch-up ended with.</summary>
/// <param name="Status">ready, needsReview or failed.</param>
/// <param name="Content">The words to store; null when nothing usable came back (failed).</param>
public sealed record ScriptOutcome(string Status, ScriptContent? Content, string Verdict, int PromptVersion, PackageReview? Review);

public sealed record AssignedChild(
    Guid ChildId, IReadOnlyList<string> MissedCompetencyIds, CompressionRationale Rationale);

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
    PackageReview? Review);

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
                p.Id, p.ClassId, p.WeekOf, p.TargetCompetencyIds, p.TargetKey, p.Language, p.Status, p.ScriptVersion, p.Hint))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SaveScriptAsync(Guid packageId, ScriptOutcome outcome, CancellationToken cancellationToken)
    {
        var package = await db.CatchUpPackages.SingleAsync(p => p.Id == packageId, cancellationToken);
        package.ScriptVersion++;
        package.PromptVersion = outcome.PromptVersion;
        package.Verdict = outcome.Verdict;
        package.Status = outcome.Status;
        package.Review = outcome.Review is null ? null : JsonSerializer.Serialize(outcome.Review, Json);
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

    public async Task<IReadOnlyList<PackageView>> ListAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken)
    {
        var packages = await db.CatchUpPackages.Where(p => p.ClassId == classId && p.WeekOf == weekOf)
            .OrderBy(p => p.Language).ThenBy(p => p.TargetKey).ToListAsync(cancellationToken);
        var assignments = await db.CatchUpAssignments.Where(a => a.ClassId == classId && a.WeekOf == weekOf)
            .ToListAsync(cancellationToken);
        return packages.Select(p => new PackageView(
                p.Id, p.Language, p.TargetCompetencyIds, p.Status, p.ScriptVersion, p.Verdict, p.Script, p.KeyWordsKn,
                p.GlossKn, p.GlossEn,
                JsonSerializer.Deserialize<ActivityData>(p.Activity, Json)!,
                JsonSerializer.Deserialize<CheckData>(p.CheckItem, Json)!,
                assignments.Where(a => a.PackageId == p.Id)
                    .Select(a => new AssignedChild(
                        a.ChildId, a.MissedCompetencyIds, JsonSerializer.Deserialize<CompressionRationale>(a.CompressionRationale, Json)!))
                    .ToList(),
                p.UpdatedAt,
                p.Review is null ? null : JsonSerializer.Deserialize<PackageReview>(p.Review, Json)))
            .ToList();
    }

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
