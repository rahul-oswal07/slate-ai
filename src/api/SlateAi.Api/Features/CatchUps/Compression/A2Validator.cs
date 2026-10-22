using System.Text.Json;

namespace SlateAi.Api.Features.CatchUps.Compression;

public static class CompressionSource
{
    /// <summary>Claude's choice, validated against the candidate set.</summary>
    public const string Ai = "ai";

    /// <summary>The nearest-prerequisite rule; never shown as an AI choice (T3).</summary>
    public const string Rule = "rule";
}

/// <param name="Reason">For the teacher's "Why this" (T3). Null when the rule chose: T3 shows its own wording.</param>
public sealed record CompressionItem(string Id, string? Reason);

/// <summary>What A2 decided after validation (guardrail 2: invalid output falls back to the rule).</summary>
/// <param name="Targets">1–2 lessons to catch up, most important first (the learning check tests the first).</param>
/// <param name="Dropped">Every other missed lesson.</param>
/// <param name="Verdict">Logged on the AiCall: valid, fallback:*, rule:no-candidates, error:*.</param>
public sealed record A2Result(
    Guid ChildId,
    IReadOnlyList<CompressionItem> Targets,
    IReadOnlyList<CompressionItem> Dropped,
    string Source,
    string Verdict);

/// <summary>Parses and checks A2's JSON against the candidate set. Pure, so every rule is unit-tested.</summary>
public static class A2Validator
{
    public const int MaxTargets = 2;
    public const int MaxReasonLength = 200;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static A2Result Validate(string json, CompressionInput input)
    {
        A2Output? output;
        try
        {
            output = JsonSerializer.Deserialize<A2Output>(json, Json);
        }
        catch (JsonException)
        {
            output = null;
        }

        if (output?.Targets is null)
        {
            return Rule(input, "fallback:invalid-output");
        }

        if (output.Targets.Count is 0 or > MaxTargets)
        {
            return Rule(input, "fallback:target-count");
        }

        var targetIds = output.Targets.Select(t => t.CompetencyId).ToList();
        if (targetIds.Any(id => id is null || !input.Candidates.Contains(id))
            || targetIds.Distinct(StringComparer.Ordinal).Count() != targetIds.Count)
        {
            return Rule(input, "fallback:target-outside-candidates");
        }

        var targets = output.Targets.Select(t => new CompressionItem(t.CompetencyId!, Clean(t.Reason))).ToList();
        var reasons = (output.Dropped ?? [])
            .Where(d => d.CompetencyId is not null)
            .GroupBy(d => d.CompetencyId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => Clean(g.First().Reason), StringComparer.Ordinal);
        var dropped = input.Missed
            .Select(m => m.Id)
            .Where(id => !targetIds.Contains(id))
            .Select(id => new CompressionItem(id, reasons.GetValueOrDefault(id)))
            .ToList();
        return new A2Result(input.ChildId, targets, dropped, CompressionSource.Ai, "valid");
    }

    /// <summary>
    /// The nearest-prerequisite rule: the head of the candidate set, or with no candidates the latest missed lesson,
    /// so every absent child still gets a catch-up. No reasons: T3 words a rule choice itself.
    /// </summary>
    public static A2Result Rule(CompressionInput input, string verdict)
    {
        var target = input.Candidates.Count > 0 ? input.Candidates[0] : input.Missed[^1].Id;
        return new A2Result(
            input.ChildId,
            [new CompressionItem(target, null)],
            input.Missed.Where(m => m.Id != target).Select(m => new CompressionItem(m.Id, null)).ToList(),
            CompressionSource.Rule,
            verdict);
    }

    /// <summary>JSON schema for structured output: targets limited to the candidates, dropped to the missed lessons.</summary>
    public static JsonElement Schema(CompressionInput input) =>
        JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "targets", "dropped" },
            ["properties"] = new Dictionary<string, object>
            {
                ["targets"] = Items(input.Candidates),
                ["dropped"] = Items(input.Missed.Select(m => m.Id).ToList()),
            },
        });

    private static Dictionary<string, object> Items(IReadOnlyList<string> ids) => new()
    {
        ["type"] = "array",
        ["items"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "competencyId", "reason" },
            ["properties"] = new Dictionary<string, object>
            {
                ["competencyId"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = ids.ToArray() },
                ["reason"] = new Dictionary<string, object> { ["type"] = "string" },
            },
        },
    };

    private static string? Clean(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null
        : text.Trim() is { Length: > MaxReasonLength } t ? t[..MaxReasonLength] : text.Trim();

    private sealed record A2Output(List<A2Item>? Targets, List<A2Item>? Dropped);

    private sealed record A2Item(string? CompetencyId, string? Reason);
}
