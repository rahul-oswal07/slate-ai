using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Practice;

namespace SlateAi.Api.Features.CatchUps.Script;

/// <summary>Everything A3 is told for one catch-up. IDs, curriculum data and numbers only (guardrail 1).</summary>
/// <param name="Language">The family's language: "kn", "te" or "ta".</param>
/// <param name="TargetIds">From A2, most important first.</param>
/// <param name="TeacherExamples">Examples she used in those lessons (A1's ExamplesUsed), already name-scrubbed.</param>
/// <param name="Hint">"simpler" or "shorter" when she asks to regenerate (FR-08).</param>
/// <param name="Feedback">The verifier's reasons from the previous attempt (one auto-retry, spec §5.1 A3).</param>
public sealed record ScriptInput(
    string Language,
    IReadOnlyList<string> TargetIds,
    PackageProblems Problems,
    IReadOnlyList<string> TeacherExamples,
    string? Hint = null,
    IReadOnlyList<string>? Feedback = null);

/// <summary>A3's words for a catch-up. The numbers stay in code (<see cref="PackageProblems"/>).</summary>
/// <param name="Script">Spoken in the family's language; no names ("your child", "your teacher", D-50).</param>
/// <param name="GlossKn">The Kannada summary she reads before approving (T3, D-09).</param>
/// <param name="Objects">The household objects the activity suggests, from <see cref="A3Validator.AllowedObjects"/> (A4 check 5).</param>
public sealed record ScriptContent(
    string Script,
    IReadOnlyList<string> KeyWordsKn,
    string ActivityInstruction,
    string ObjectOption,
    string CheckPrompt,
    string GlossKn,
    string GlossEn,
    IReadOnlyList<string> Objects);

/// <param name="Content">Null when the output was refused.</param>
/// <param name="Verdict">Logged on the AiCall: valid, fallback:* or error:*.</param>
public sealed record A3Result(ScriptContent? Content, string Verdict);

/// <summary>
/// Schema-level checks on A3's JSON (guardrail 2). Pure. Content checks (length after TTS, script block, numbers,
/// Haiku rubric) are the verifier's (A4, T-10).
/// </summary>
public static partial class A3Validator
{
    public const int KeyWords = 3;
    public const int MaxScriptWords = 160;
    public const int MaxField = 600;

    /// <summary>Indic scripts take several characters a word (vowel signs), so the script's limit is in words.</summary>
    public const int MaxScriptChars = 2000;

    /// <summary>Household objects the activity may use (spec §3.3).</summary>
    public static readonly string[] AllowedObjects =
        ["bottle caps", "sticks", "stones", "tamarind seeds", "bangles", "rotis", "spoons"];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The Kannada words A3 may choose from: the targets' textbook words.</summary>
    public static IReadOnlyList<string> KeyWordChoices(ScriptInput input, CurriculumGraph curriculum) =>
        input.TargetIds.SelectMany(id => curriculum.Get(id)?.KeyWordsKn ?? []).Distinct(StringComparer.Ordinal).ToList();

    /// <param name="names">The class's child and teacher names: never allowed in the output.</param>
    public static A3Result Validate(string json, ScriptInput input, CurriculumGraph curriculum, IEnumerable<string> names)
    {
        A3Output? output;
        try
        {
            output = JsonSerializer.Deserialize<A3Output>(json, Json);
        }
        catch (JsonException)
        {
            output = null;
        }

        string?[] texts =
        [
            output?.Script, output?.Activity?.Instruction, output?.Activity?.ObjectOption, output?.CheckPrompt,
            output?.GlossKn, output?.GlossEn,
        ];
        if (output is null || texts.Any(string.IsNullOrWhiteSpace))
        {
            return Refused("fallback:invalid-output");
        }

        if (texts.Skip(1).Any(t => t!.Trim().Length > MaxField)
            || output.Script!.Trim().Length > MaxScriptChars
            || Words(output.Script!) > MaxScriptWords)
        {
            return Refused("fallback:too-long");
        }

        var choices = KeyWordChoices(input, curriculum);
        var words = (output.KeyWordsKn ?? []).Select(w => w.Trim()).ToList();
        if (words.Count != KeyWords || words.Distinct(StringComparer.Ordinal).Count() != KeyWords || !words.All(choices.Contains))
        {
            return Refused("fallback:key-words");
        }

        if (UnsafeText(texts!, names) is { } problem)
        {
            return Refused(problem);
        }

        return new A3Result(
            new ScriptContent(
                output.Script!.Trim(), words, output.Activity!.Instruction!.Trim(), output.Activity.ObjectOption!.Trim(),
                output.CheckPrompt!.Trim(), output.GlossKn!.Trim(), output.GlossEn!.Trim(),
                (output.Activity.Objects ?? []).Select(o => o.Trim()).Distinct().ToList()),
            "valid");
    }

    public static A3Result Refused(string verdict) => new(null, verdict);

    /// <summary>
    /// "fallback:placeholder" if any text has braces or markup, "fallback:name" if it has a class name (Latin words,
    /// exact), else null. Also A4's names check (spec §5.4 check 6).
    /// </summary>
    public static string? UnsafeText(IEnumerable<string> texts, IEnumerable<string> names)
    {
        var all = texts.ToList();
        if (all.Any(t => t.IndexOfAny(['{', '}', '<', '>']) >= 0))
        {
            return "fallback:placeholder";
        }

        var nameSet = names.SelectMany(n => n.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(n => n.Length >= 3)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return all.Any(t => LatinWord().Matches(t).Any(m => nameSet.Contains(m.Value))) ? "fallback:name" : null;
    }

    /// <summary>JSON schema for structured output; key words limited to the targets' textbook words.</summary>
    public static JsonElement Schema(IReadOnlyList<string> keyWordChoices)
    {
        var text = new Dictionary<string, object> { ["type"] = "string" };
        return JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "script", "keyWordsKn", "activity", "checkPrompt", "glossKn", "glossEn" },
            ["properties"] = new Dictionary<string, object>
            {
                ["script"] = text,
                ["keyWordsKn"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = keyWordChoices.ToArray() },
                },
                ["activity"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new[] { "instruction", "objectOption", "objects" },
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["instruction"] = text,
                        ["objectOption"] = text,
                        ["objects"] = new Dictionary<string, object>
                        {
                            ["type"] = "array",
                            ["items"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = AllowedObjects },
                        },
                    },
                },
                ["checkPrompt"] = text,
                ["glossKn"] = text,
                ["glossEn"] = text,
            },
        });
    }

    private static int Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    [GeneratedRegex(@"[A-Za-z]+", RegexOptions.CultureInvariant)]
    private static partial Regex LatinWord();

    private sealed record A3Output(
        string? Script, List<string>? KeyWordsKn, A3Activity? Activity, string? CheckPrompt, string? GlossKn, string? GlossEn);

    private sealed record A3Activity(string? Instruction, string? ObjectOption, List<string>? Objects);
}
