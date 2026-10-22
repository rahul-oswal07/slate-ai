using System.Text.Json;
using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.Lessons.Voice;

/// <summary>What A1 decided after validation (guardrail 2: invalid output falls back to rules).</summary>
/// <param name="Status">"mapped", "low" (T2 asks her to choose) or "unclear" (T2 sends her to the list).</param>
/// <param name="Verdict">Logged on the AiCall: valid, fallback:low-confidence, fallback:no-valid-id, fallback:invalid-output.</param>
public sealed record A1Result(
    string Status,
    IReadOnlyList<string> CompetencyIds,
    string? SummaryKn,
    string? SummaryEn,
    IReadOnlyList<string> ExamplesUsed,
    double? Confidence,
    string Verdict);

/// <summary>Parses and checks A1's JSON against the curriculum. Pure, so every rule is unit-tested.</summary>
public static class A1Validator
{
    public const double ConfidenceThreshold = 0.7;
    public const int MaxLessons = 3;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static A1Result Validate(string json, CurriculumGraph curriculum)
    {
        A1Output? output;
        try
        {
            output = JsonSerializer.Deserialize<A1Output>(json, Json);
        }
        catch (JsonException)
        {
            output = null;
        }

        if (output is null)
        {
            return Unclear("fallback:invalid-output");
        }

        var ids = (output.CompetencyIds ?? [])
            .Where(curriculum.Contains)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxLessons)
            .ToList();
        if (ids.Count == 0)
        {
            return Unclear("fallback:no-valid-id");
        }

        var confidence = output.Confidence is >= 0 and <= 1 ? output.Confidence : null;
        var low = confidence is null or < ConfidenceThreshold;
        return new A1Result(
            low ? LessonValues.Low : LessonValues.Mapped,
            ids,
            Clean(output.SummaryKn),
            Clean(output.SummaryEn),
            (output.ExamplesUsed ?? []).Select(Clean).OfType<string>().Take(5).ToList(),
            confidence,
            low ? "fallback:low-confidence" : "valid");
    }

    public static A1Result Unclear(string verdict) => new(LessonValues.Unclear, [], null, null, [], null, verdict);

    private static string? Clean(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim() is { Length: > 300 } t ? t[..300] : text.Trim();

    /// <summary>JSON schema for structured output; IDs constrained to the curriculum.</summary>
    public static JsonElement Schema(CurriculumGraph curriculum) =>
        JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "competencyIds", "confidence", "summaryKn", "summaryEn", "examplesUsed" },
            ["properties"] = new Dictionary<string, object>
            {
                ["competencyIds"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["enum"] = curriculum.All.Select(c => c.Id).ToArray(),
                    },
                },
                ["confidence"] = new Dictionary<string, object> { ["type"] = "number" },
                ["summaryKn"] = new Dictionary<string, object> { ["type"] = "string" },
                ["summaryEn"] = new Dictionary<string, object> { ["type"] = "string" },
                ["examplesUsed"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object> { ["type"] = "string" },
                },
            },
        });

    private sealed record A1Output(
        List<string>? CompetencyIds, double? Confidence, string? SummaryKn, string? SummaryEn, List<string>? ExamplesUsed);
}
