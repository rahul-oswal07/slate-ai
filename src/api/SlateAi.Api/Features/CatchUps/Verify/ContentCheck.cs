using System.Text.Encodings.Web;
using System.Text.Json;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.CatchUps.Verify;

/// <summary>
/// A4's LLM check (spec §5.4, Haiku): the script teaches the target, suits an 8-year-old, never shames, and the
/// Kannada/English summary matches it. Names never reach it (the text was already checked for them).
/// </summary>
public sealed class ContentCheck(ILlmClient llm, IAiCallStore aiCalls, PromptLibrary prompts, CurriculumGraph curriculum)
{
    public const string PromptId = "a4-check";
    public const string Name = "content";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly string[] Fields = ["teachesTarget", "ageAppropriate", "noShaming", "glossMatches"];

    private static readonly Dictionary<string, string> Problems = new()
    {
        ["teachesTarget"] = "doesn't teach the lesson",
        ["ageAppropriate"] = "too hard for the child's age",
        ["noShaming"] = "speaks negatively about the absence",
        ["glossMatches"] = "the summary doesn't match the script",
    };

    public async Task<VerifierCheck> RunAsync(VerifyInput input, CancellationToken cancellationToken)
    {
        var request = new LlmRequest(prompts.Get(PromptId), UserContent(input), Schema(), MaxTokens: 2048);
        LlmResponse response;
        try
        {
            response = await llm.CompleteJsonAsync(request, cancellationToken);
        }
        catch (LlmException)
        {
            // Not passed, so the catch-up goes to review rather than to ready.
            return new VerifierCheck(Name, false, "the content check could not run");
        }

        var check = Read(response.Json);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, check.Passed ? "pass" : "fail:" + check.Detail, cancellationToken);
        }

        return check;
    }

    internal static VerifierCheck Read(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var failed = Fields.Where(f => !root.TryGetProperty(f, out var v) || v.ValueKind != JsonValueKind.True).ToList();
            var reason = root.TryGetProperty("reason", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString()?.Trim() : null;
            if (failed.Count == 0)
            {
                return new VerifierCheck(Name, true, "teaches the lesson, suits the age, kind, summary matches");
            }

            var detail = string.Join("; ", failed.Select(f => Problems[f]));
            return new VerifierCheck(Name, false, string.IsNullOrWhiteSpace(reason) ? detail : $"{detail} ({Cut(reason)})");
        }
        catch (JsonException)
        {
            return new VerifierCheck(Name, false, "the content check gave no answer");
        }
    }

    internal string UserContent(VerifyInput input) =>
        JsonSerializer.Serialize(new
        {
            language = input.Language,
            targets = input.TargetIds.Select(curriculum.Get).OfType<Competency>().Select(c => new { c.NameEn, c.NameKn }),
            script = input.Content.Script,
            activity = input.Content.ActivityInstruction,
            objectOption = input.Content.ObjectOption,
            glossKn = input.Content.GlossKn,
            glossEn = input.Content.GlossEn,
        }, Json);

    private static JsonElement Schema()
    {
        var flag = new Dictionary<string, object> { ["type"] = "boolean" };
        return JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "teachesTarget", "ageAppropriate", "noShaming", "glossMatches", "reason" },
            ["properties"] = new Dictionary<string, object>
            {
                ["teachesTarget"] = flag,
                ["ageAppropriate"] = flag,
                ["noShaming"] = flag,
                ["glossMatches"] = flag,
                ["reason"] = new Dictionary<string, object> { ["type"] = "string" },
            },
        });
    }

    private static string Cut(string text) => text.Length > 160 ? text[..160] : text;
}

/// <summary>The verifier's decision on one attempt (stored on the package as its review).</summary>
public sealed record VerifierResult(bool Passed, IReadOnlyList<VerifierCheck> Checks)
{
    /// <summary>The failed checks' details, for the retry's feedback and the teacher's "Needs review" line.</summary>
    public IReadOnlyList<string> Problems => Checks.Where(c => !c.Passed).Select(c => $"{c.Name}: {c.Detail}").ToList();

    /// <summary>Short verdict code, e.g. "verifier:length".</summary>
    public string Verdict => Passed ? "verified" : "verifier:" + string.Join(",", Checks.Where(c => !c.Passed).Select(c => c.Name));
}

/// <summary>A4 (spec §5.4): code checks first; the Haiku check only when they all pass.</summary>
public sealed class Verifier(ContentCheck content, CurriculumGraph curriculum, Microsoft.Extensions.Options.IOptions<VerifierOptions> options)
{
    public async Task<VerifierResult> VerifyAsync(VerifyInput input, CancellationToken cancellationToken)
    {
        var checks = CodeChecks.Run(input, curriculum, options.Value).ToList();
        if (checks.All(c => c.Passed))
        {
            checks.Add(await content.RunAsync(input, cancellationToken));
        }

        return new VerifierResult(checks.All(c => c.Passed), checks);
    }
}
