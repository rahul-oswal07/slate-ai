using System.Text.Json;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Practice;

namespace SlateAi.Api.Features.CatchUps;

/// <summary>A catch-up to create: one per (targets, language) per week, with its code-made numbers.</summary>
/// <param name="Problems">Null when no target has a practice rule: the package is created as failed.</param>
public sealed record PlannedPackage(
    IReadOnlyList<string> TargetIds, string TargetKey, string Language, PackageProblems? Problems);

/// <summary>One child's place in a catch-up, with A2's reasons.</summary>
public sealed record PlannedAssignment(
    Guid ChildId, string TargetKey, string Language, IReadOnlyList<string> MissedCompetencyIds, string RationaleJson);

/// <summary>What A2 said for one child, as stored on the assignment ("Why this" on T3).</summary>
public sealed record CompressionRationale(
    IReadOnlyList<CompressionItem> Targets, IReadOnlyList<CompressionItem> Dropped, string Source, string Verdict);

/// <summary>
/// Groups children into catch-ups (FR-07: one per target set and home language) and makes each one's numbers.
/// Pure code, no LLM.
/// </summary>
public static class CatchUpPlanner
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string TargetKey(IEnumerable<string> targets) => string.Join("+", targets);

    /// <summary>The seed for a catch-up's numbers, so regenerating the words keeps the same sums.</summary>
    public static string SeedKey(Guid classId, DateOnly weekOf, string targetKey, string language) =>
        $"{classId}|{weekOf:yyyy-MM-dd}|{targetKey}|{language}";

    /// <param name="languages">Each child's home language.</param>
    public static (IReadOnlyList<PlannedPackage> Packages, IReadOnlyList<PlannedAssignment> Assignments) Plan(
        Guid classId,
        DateOnly weekOf,
        IReadOnlyList<A2Result> results,
        IReadOnlyDictionary<Guid, string> languages,
        CurriculumGraph curriculum)
    {
        var assignments = results
            .Where(r => languages.ContainsKey(r.ChildId))
            .Select(r => new PlannedAssignment(
                r.ChildId,
                TargetKey(r.Targets.Select(t => t.Id)),
                languages[r.ChildId],
                r.Targets.Concat(r.Dropped).Select(i => i.Id).ToList(), // A2 places every missed lesson in one or the other
                JsonSerializer.Serialize(new CompressionRationale(r.Targets, r.Dropped, r.Source, r.Verdict), Json)))
            .ToList();

        var packages = assignments
            .GroupBy(a => (a.TargetKey, a.Language))
            .Select(g =>
            {
                var targets = g.Key.TargetKey.Split('+');
                return new PlannedPackage(
                    targets,
                    g.Key.TargetKey,
                    g.Key.Language,
                    PackagePractice.For(targets, curriculum, SeedKey(classId, weekOf, g.Key.TargetKey, g.Key.Language)));
            })
            .OrderBy(p => p.Language, StringComparer.Ordinal)
            .ThenBy(p => p.TargetKey, StringComparer.Ordinal)
            .ToList();

        return (packages, assignments);
    }
}
