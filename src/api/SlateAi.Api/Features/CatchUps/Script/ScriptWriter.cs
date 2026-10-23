using System.Text.Encodings.Web;
using System.Text.Json;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.CatchUps.Script;

/// <summary>
/// A3, the catch-up script writer (FR-07): the words of one catch-up in the family's language, around numbers that
/// code already chose. Opus 5.5, structured output, validated before anything is stored (guardrail 2).
/// </summary>
public sealed class ScriptWriter(ILlmClient llm, IAiCallStore aiCalls, PromptLibrary prompts, CurriculumGraph curriculum)
{
    public const string PromptId = "a3-script";

    // Kannada, Telugu and "−" as themselves rather than \uXXXX escapes: fewer tokens, and this JSON never reaches HTML.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["kn"] = "Kannada",
        ["te"] = "Telugu",
        ["ta"] = "Tamil",
        ["en"] = "English",
    };

    /// <param name="names">The class's names, only to refuse output that contains one; never sent.</param>
    public async Task<A3Result> WriteAsync(ScriptInput input, IEnumerable<string> names, CancellationToken cancellationToken)
    {
        var choices = A3Validator.KeyWordChoices(input, curriculum);
        if (choices.Count < A3Validator.KeyWords)
        {
            return A3Validator.Refused("fallback:no-key-words");
        }

        // 8192: Opus thinks before writing six fields in an Indic script; 4096 was truncated in the eval.
        var request = new LlmRequest(prompts.Get(PromptId), UserContent(input), A3Validator.Schema(choices), MaxTokens: 8192);
        LlmResponse response;
        try
        {
            response = await llm.CompleteJsonAsync(request, cancellationToken);
        }
        catch (LlmException e)
        {
            // The AiCall row already records the failure.
            return A3Validator.Refused($"error:{e.Failure}");
        }

        var result = A3Validator.Validate(response.Json, input, curriculum, names);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, result.Verdict, cancellationToken);
        }

        return result;
    }

    /// <summary>Curriculum data and code-made numbers only: no names, no child IDs (guardrail 1).</summary>
    internal string UserContent(ScriptInput input)
    {
        var p = input.Problems;
        return JsonSerializer.Serialize(new
        {
            language = input.Language,
            languageName = LanguageNames.GetValueOrDefault(input.Language, input.Language),
            targets = input.TargetIds.Select(curriculum.Get).OfType<Competency>().Select(c => new
            {
                c.Id,
                c.NameEn,
                c.NameKn,
                keyWordsKn = c.KeyWordsKn ?? [],
                textbookPage = c.TextbookPage,
            }),
            checkTests = p.CompetencyId,
            activity = new { p.Activity.Kind, p.Activity.Question, p.Activity.Numbers, p.Activity.Answer },
            check = new { p.Check.Kind, p.Check.Question, p.Check.Answer },
            teacherExamples = input.TeacherExamples,
            allowedObjects = A3Validator.AllowedObjects,
            hint = input.Hint,
            previousAttemptProblems = input.Feedback ?? [],
        }, Json);
    }
}
