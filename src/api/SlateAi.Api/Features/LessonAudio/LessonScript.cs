using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;

namespace SlateAi.Api.Features.LessonAudio;

/// <param name="Script">Spoken in the family's language; no names ("your child", "your teacher", D-50). Null when refused.</param>
/// <param name="Verdict">Logged on the AiCall and kept on the row: valid, fallback:* or error:*.</param>
public sealed record LessonScriptResult(string? Script, string Verdict);

/// <summary>
/// Schema-level and content checks on A8's JSON (guardrail 2). Pure. The clip is made once per (lesson, language) and
/// shared, so nothing in it may depend on a child or a class: no names, no braces, only numbers from the curriculum.
/// </summary>
public static partial class LessonScriptValidator
{
    public const int MaxWords = 80;
    public const int MaxChars = 1500;

    /// <summary>Share of the script's letters that must be in the family's script (the A3 limit).</summary>
    public const double MinScriptShare = 0.8;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static JsonElement Schema() => JsonSerializer.SerializeToElement(new Dictionary<string, object>
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new[] { "script" },
        ["properties"] = new Dictionary<string, object> { ["script"] = new Dictionary<string, object> { ["type"] = "string" } },
    });

    /// <param name="names">The requesting class's child names, only to refuse output that contains one; never sent.</param>
    public static LessonScriptResult Validate(
        string json, string language, IReadOnlyList<Competency> lessons, IEnumerable<string> names)
    {
        string? script;
        try
        {
            script = JsonSerializer.Deserialize<Output>(json, Json)?.Script?.Trim();
        }
        catch (JsonException)
        {
            script = null;
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            return Refused("fallback:invalid-output");
        }

        if (script.Length > MaxChars || script.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > MaxWords)
        {
            return Refused("fallback:too-long");
        }

        if (A3Validator.UnsafeText([script], names) is { } problem)
        {
            return Refused(problem);
        }

        if (CodeChecks.ScriptShare(script, language) is not var (share, _) || share < MinScriptShare)
        {
            return Refused("fallback:script");
        }

        var allowed = AllowedNumbers(lessons);
        return Digits().Matches(script).Any(m => !allowed.Contains(m.Value)) ? Refused("fallback:numbers") : new LessonScriptResult(script, "valid");
    }

    public static LessonScriptResult Refused(string verdict) => new(null, verdict);

    /// <summary>The numbers the script may say: those in the lessons' textbook examples and their page (guardrail 4).</summary>
    public static HashSet<string> AllowedNumbers(IReadOnlyList<Competency> lessons)
    {
        var numbers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var lesson in lessons)
        {
            foreach (var match in lesson.Examples.SelectMany(e => Digits().Matches(e)))
            {
                numbers.Add(match.Value);
            }

            if (lesson.TextbookPage is { } page)
            {
                numbers.Add(page.Page.ToString());
                if (page.Part is { } part)
                {
                    numbers.Add(part.ToString());
                }
            }
        }

        return numbers;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();

    private sealed record Output(string? Script);
}

/// <summary>
/// A8, the lesson-recap writer: a few spoken sentences about what the class learned, in the family's language. Curriculum
/// data only, so the clip is the same for every class (guardrail 1). Opus 5.5 via the Script model key, structured output.
/// </summary>
public sealed class LessonScriptWriter(ILlmClient llm, IAiCallStore aiCalls, PromptLibrary prompts)
{
    public const string PromptId = "a8-lesson-recap";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <param name="names">The requesting class's child names, only to refuse output that contains one; never sent.</param>
    public async Task<LessonScriptResult> WriteAsync(
        string language,
        IReadOnlyList<Competency> lessons,
        IReadOnlyList<string>? feedback,
        IEnumerable<string> names,
        CancellationToken cancellationToken)
    {
        var request = new LlmRequest(prompts.Get(PromptId), UserContent(language, lessons, feedback), LessonScriptValidator.Schema(), MaxTokens: 4096);
        LlmResponse response;
        try
        {
            response = await llm.CompleteJsonAsync(request, cancellationToken);
        }
        catch (LlmException e)
        {
            // The AiCall row already records the failure.
            return LessonScriptValidator.Refused($"error:{e.Failure}");
        }

        var result = LessonScriptValidator.Validate(response.Json, language, lessons, names);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, result.Verdict, cancellationToken);
        }

        return result;
    }

    /// <summary>Curriculum data only: no names, no child or class IDs, no teacher examples (guardrail 1).</summary>
    internal static string UserContent(string language, IReadOnlyList<Competency> lessons, IReadOnlyList<string>? feedback) =>
        JsonSerializer.Serialize(new
        {
            language,
            languageName = ScriptWriter.LanguageNames.GetValueOrDefault(language, language),
            lessons = lessons.Select(c => new
            {
                c.Id,
                name = c.Name(language),
                c.NameEn,
                c.NameKn,
                examples = c.Examples,
                keyWordsKn = c.KeyWordsKn ?? [],
                textbookPage = c.TextbookPage,
            }),
            previousAttemptProblems = feedback ?? [],
        }, Json);
}
