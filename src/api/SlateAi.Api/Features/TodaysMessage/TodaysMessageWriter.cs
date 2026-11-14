using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.LessonAudio;

namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>A9's text for one language: the concept and the activity steps.</summary>
public sealed record TodaysText(string Concept, IReadOnlyList<string> Steps);

/// <param name="Texts">By language; null when refused.</param>
/// <param name="Verdict">Logged on the AiCall and kept on the row: valid, fallback:* or error:*.</param>
/// <param name="Detail">What exactly was wrong (e.g. the numbers not allowed), for the retry's feedback; no names.</param>
public sealed record TodaysWriteResult(IReadOnlyDictionary<string, TodaysText>? Texts, string Verdict, string? Detail = null);

/// <summary>
/// Schema-level and content checks on A9's JSON (guardrail 2). Pure. Invalid output never passes: the generator retries
/// once with the problem, then the row fails and families get the plain evening message.
/// </summary>
public static partial class TodaysMessageValidator
{
    public const int MaxSentences = 3;
    public const int MaxConceptWords = 60;
    public const int MaxSteps = 3;
    public const int MaxStepWords = 30;
    public const int MaxChars = 700;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static JsonElement Schema() => JsonSerializer.SerializeToElement(new Dictionary<string, object>
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new[] { "languages" },
        ["properties"] = new Dictionary<string, object>
        {
            ["languages"] = new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new[] { "language", "concept", "activity" },
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["language"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["concept"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["activity"] = new Dictionary<string, object>
                        {
                            ["type"] = "array",
                            ["items"] = new Dictionary<string, object> { ["type"] = "string" },
                        },
                    },
                },
            },
        },
    });

    /// <param name="names">The class's child names, only to refuse output that contains one; never sent.</param>
    public static TodaysWriteResult Validate(
        string json, IReadOnlyList<string> languages, IReadOnlyList<Competency> lessons, TodaysPicture picture, IEnumerable<string> names)
    {
        Output? output;
        try
        {
            output = JsonSerializer.Deserialize<Output>(json, Json);
        }
        catch (JsonException)
        {
            output = null;
        }

        var items = output?.Languages ?? [];
        if (items.Any(i => i.Language is null || i.Concept is null || i.Activity is null)
            || items.Select(i => i.Language).Distinct().Count() != items.Count
            || !languages.Order().SequenceEqual(items.Select(i => i.Language!).Order()))
        {
            return Refused("fallback:invalid-output", $"write exactly these languages, once each: {string.Join(", ", languages)}");
        }

        var allowed = AllowedNumbers(lessons, picture);
        var namesList = names.ToList();
        var texts = new Dictionary<string, TodaysText>();
        foreach (var item in items)
        {
            var concept = item.Concept!.Trim();
            var steps = item.Activity!.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (concept.Length == 0 || steps.Count == 0)
            {
                return Refused("fallback:invalid-output");
            }

            if (steps.Count > MaxSteps || steps.Any(s => Words(s) > MaxStepWords)
                || Sentences(concept) > MaxSentences || Words(concept) > MaxConceptWords
                || concept.Length + steps.Sum(s => s.Length) > MaxChars)
            {
                return Refused("fallback:too-long",
                    $"{item.Language}: concept {Sentences(concept)} sentences, {Words(concept)} words; {steps.Count} steps, longest {steps.Max(Words)} words");
            }

            var all = steps.Prepend(concept).ToList();
            if (A3Validator.UnsafeText(all, namesList) is { } problem)
            {
                return Refused(problem);
            }

            if (CodeChecks.ScriptShare(string.Join(' ', all), item.Language!) is not var (share, _)
                || share < LessonScriptValidator.MinScriptShare)
            {
                return Refused("fallback:script", $"{item.Language}: too few letters in the language's own script");
            }

            var extra = all.SelectMany(t => Digits().Matches(t)).Select(m => m.Value).Where(n => !allowed.Contains(n)).Distinct().ToList();
            if (extra.Count > 0)
            {
                return Refused("fallback:numbers", $"{item.Language}: numbers not allowed: {string.Join(", ", extra)}");
            }

            texts[item.Language!] = new TodaysText(concept, steps);
        }

        return new TodaysWriteResult(texts, "valid");
    }

    public static TodaysWriteResult Refused(string verdict, string? detail = null) => new(null, verdict, detail);

    /// <summary>
    /// The numbers A9 may write (guardrail 4): the lesson's examples and page, the book's exercise numbers with their tens
    /// and ones (so "7 bundles and 5 loose sticks" for 75 is fine), and 10 for a bundle. Nothing else.
    /// </summary>
    public static HashSet<string> AllowedNumbers(IReadOnlyList<Competency> lessons, TodaysPicture picture)
    {
        var numbers = LessonScriptValidator.AllowedNumbers(lessons);
        var book = lessons
            .SelectMany(l => (l.Exercises?.Compare ?? []).Concat(l.Exercises?.Extremes ?? []))
            .SelectMany(x => x)
            .Append(picture.A)
            .Append(picture.B);
        foreach (var n in book)
        {
            numbers.Add(n.ToString());
            numbers.Add((n / 10).ToString());
            numbers.Add((n % 10).ToString());
        }

        numbers.Add("10");
        return numbers;
    }

    private static int Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Sentence ends: . ! ? and the Devanagari danda, each followed by a space or the end.</summary>
    private static int Sentences(string text) => Math.Max(1, SentenceEnds().Matches(text.Trim()).Count);

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();

    [GeneratedRegex(@"[.!?।](\s|$)")]
    private static partial Regex SentenceEnds();

    private sealed record Output(List<Item>? Languages);

    private sealed record Item(string? Language, string? Concept, List<string>? Activity);
}

/// <summary>
/// A9, today's message writer (D-91): the concept and a parent-led activity for today's lesson, in Kannada and each
/// absent family's language, around numbers chosen by code. Curriculum data only (guardrail 1). Opus via the Script key.
/// </summary>
public sealed class TodaysMessageWriter(ILlmClient llm, IAiCallStore aiCalls, PromptLibrary prompts)
{
    public const string PromptId = "a9-todays-message";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <param name="names">The class's child names, only to refuse output that contains one; never sent.</param>
    public async Task<TodaysWriteResult> WriteAsync(
        IReadOnlyList<string> languages,
        IReadOnlyList<Competency> lessons,
        TodaysPicture picture,
        IReadOnlyList<string>? feedback,
        IEnumerable<string> names,
        CancellationToken cancellationToken)
    {
        var request = new LlmRequest(
            prompts.Get(PromptId), UserContent(languages, lessons, picture, feedback), TodaysMessageValidator.Schema(), MaxTokens: 8192);
        LlmResponse response;
        try
        {
            response = await llm.CompleteJsonAsync(request, cancellationToken);
        }
        catch (LlmException e)
        {
            // The AiCall row already records the failure.
            return TodaysMessageValidator.Refused($"error:{e.Failure}");
        }

        var result = TodaysMessageValidator.Validate(response.Json, languages, lessons, picture, names);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, result.Verdict, cancellationToken);
        }

        return result;
    }

    /// <summary>Curriculum data and code-chosen numbers only: no names, no child or class IDs (guardrail 1).</summary>
    public static string UserContent(
        IReadOnlyList<string> languages, IReadOnlyList<Competency> lessons, TodaysPicture picture, IReadOnlyList<string>? feedback) =>
        JsonSerializer.Serialize(new
        {
            languages = languages.Select(l => new { code = l, name = ScriptWriter.LanguageNames.GetValueOrDefault(l, l) }),
            lessons = lessons.Select(c => new
            {
                c.Id,
                c.NameEn,
                c.NameKn,
                names = languages.ToDictionary(l => l, l => c.Name(l)),
                examples = c.Examples,
                keyWordsKn = c.KeyWordsKn ?? [],
                textbookPage = c.TextbookPage,
                exercises = c.Exercises,
            }),
            pictureQuestion = new { picture.A, picture.B },
            previousAttemptProblems = feedback ?? [],
        }, Json);
}
