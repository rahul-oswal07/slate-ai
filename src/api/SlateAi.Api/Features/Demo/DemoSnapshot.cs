using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Demo;

/// <summary>An audio clip inside a snapshot. <see cref="Shared"/> clips (the disclosure) belong to no package.</summary>
public sealed record SnapshotClip(
    Guid Id, string Kind, string Language, bool Shared, string? TextHash, string ContentType, string Base64, double Seconds);

/// <summary>
/// A generated catch-up saved as a file (<c>data/seed/demo-catchup.json</c>) so the demo Reset can restore it as Ready
/// without calling Claude or Sarvam (risk R5). Made once from a live run with <c>GET /api/demo/snapshot/{id}</c> in
/// Development. Holds no child or teacher names: scripts use placeholders (guardrail 1).
/// </summary>
public sealed partial record DemoSnapshot(
    IReadOnlyList<string> TargetCompetencyIds,
    string TargetKey,
    string Language,
    int? PromptVersion,
    string? Verdict,
    string? Script,
    IReadOnlyList<string> KeyWordsKn,
    string? GlossKn,
    string? GlossEn,
    string Activity,
    string CheckItem,
    string? Review,
    string? AudioUrl,
    string? AudioSegments,
    IReadOnlyList<string> MissedCompetencyIds,
    string CompressionRationale,
    IReadOnlyList<SnapshotClip> Clips)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static DemoSnapshot From(CatchUpPackage package, CatchUpAssignment? assignment, IReadOnlyList<AudioClip> clips) =>
        new(
            package.TargetCompetencyIds, package.TargetKey, package.Language, package.PromptVersion, package.Verdict,
            package.Script, package.KeyWordsKn, package.GlossKn, package.GlossEn, package.Activity, package.CheckItem,
            package.Review, package.AudioUrl, package.AudioSegments,
            assignment?.MissedCompetencyIds ?? package.TargetCompetencyIds,
            assignment?.CompressionRationale ?? "{}",
            clips.Select(c => new SnapshotClip(
                c.Id, c.Kind, c.Language, c.PackageId is null, c.TextHash, c.ContentType,
                Convert.ToBase64String(c.Bytes), c.Seconds)).ToList());

    /// <summary>The clip IDs a package's audio points at (<c>/api/audio/{id}</c> URLs).</summary>
    public static IReadOnlySet<Guid> ClipIds(CatchUpPackage package) =>
        GuidPattern().Matches($"{package.AudioUrl} {package.AudioSegments}")
            .Select(m => Guid.Parse(m.Value))
            .ToHashSet();

    /// <summary>Replaces old clip IDs with the ones the restored clips got.</summary>
    public static string? Remap(string? text, IReadOnlyDictionary<Guid, Guid> ids) =>
        text is null
            ? null
            : GuidPattern().Replace(text, m =>
                Guid.TryParse(m.Value, out var old) && ids.TryGetValue(old, out var id) ? id.ToString() : m.Value);

    /// <summary>The snapshot at <paramref name="file"/>, or null when it hasn't been made yet.</summary>
    public static async Task<DemoSnapshot?> LoadAsync(string file, CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, file);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DemoSnapshot>(stream, Json, cancellationToken);
    }

    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();
}
