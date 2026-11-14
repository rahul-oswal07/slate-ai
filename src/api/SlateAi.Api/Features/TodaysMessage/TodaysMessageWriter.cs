using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.LessonAudio;

namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>The text for one language: the concept, the activity steps and, when she wrote one, her note.</summary>
public sealed record TodaysText(string Concept, IReadOnlyList<string> Steps, string? Note = null);

/// <summary>Her own Kannada (D-92), kept word for word; A9 writes the other languages to say the same.</summary>
public sealed record TeacherKannada(string Concept, IReadOnlyList<string> Steps, string? Note);

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
    public const int MaxNoteSentences = 2;
    public const int MaxNoteWords = 40;
    public const int MaxChars = 900;

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
                        ["note"] = new Dictionary<string, object> { ["type"] = "string" },
                    },
                },
            },
        },
    });

    /// <param name="names">The class's child names, only to refuse output that contains one; never sent.</param>
    /// <param name="expectNote">Her note is being translated: every language must have one.</param>
    public static TodaysWriteResult Validate(
        string json,
        IReadOnlyList<string> languages,
        IReadOnlyList<Competency> lessons,
        TodaysPicture picture,
        IEnumerable<string> names,
        bool expectNote = false)
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
        if (items.Any(i => i.Language is null || i.Concept is null || i.Activity is null || (expectNote && string.IsNullOrWhiteSpace(i.Note)))
            || items.Select(i => i.Language).Distinct().Count() != items.Count
            || !languages.Order().SequenceEqual(items.Select(i => i.Language!).Order()))
        {
            return Refused("fallback:invalid-output", $"write exactly these languages, once each{(expectNote ? ", each with the note" : "")}: {string.Join(", ", languages)}");
        }

        var allowed = AllowedNumbers(lessons, picture);
        var namesList = names.ToList();
        var texts = new Dictionary<string, TodaysText>();
        foreach (var item in items)
        {
            var text = new TodaysText(
                item.Concept!.Trim(),
                item.Activity!.Select(s => s.Trim()).Where(s => s.Length > 0).ToList(),
                expectNote ? item.Note!.Trim() : null);
            if (Check(item.Language!, text, allowed, namesList) is { } refused)
            {
                return refused;
            }

            texts[item.Language!] = text;
        }

        return new TodaysWriteResult(texts, "valid");
    }

    /// <summary>
    /// The checks every language's text goes through, A9's and hers alike (D-92): not empty, the length limits, no names,
    /// braces or signs, the language's own script, only the allowed numbers. Null when it passes.
    /// </summary>
    public static TodaysWriteResult? Check(string language, TodaysText text, IReadOnlySet<string> allowed, IReadOnlyList<string> names)
    {
        var (concept, steps, note) = (text.Concept, text.Steps, text.Note);
        if (concept.Length == 0 || steps.Count == 0 || note is { Length: 0 })
        {
            return Refused("fallback:invalid-output", $"{language}: empty concept, activity or note");
        }

        if (steps.Count > MaxSteps || steps.Any(s => Words(s) > MaxStepWords)
            || Sentences(concept) > MaxSentences || Words(concept) > MaxConceptWords
            || (note is not null && (Sentences(note) > MaxNoteSentences || Words(note) > MaxNoteWords))
            || concept.Length + steps.Sum(s => s.Length) + (note?.Length ?? 0) > MaxChars)
        {
            return Refused("fallback:too-long",
                $"{language}: concept {Sentences(concept)} sentences, {Words(concept)} words; {steps.Count} steps, longest {steps.Max(Words)} words"
                + (note is null ? "" : $"; note {Sentences(note)} sentences, {Words(note)} words"));
        }

        var all = steps.Prepend(concept).Concat(note is null ? [] : [note]).ToList();
        if (A3Validator.UnsafeText(all, names) is { } problem)
        {
            return Refused(problem, $"{language}: {(problem == "fallback:name" ? "a child's name" : "braces, angle brackets or markup")}");
        }

        if (CodeChecks.ScriptShare(string.Join(' ', all), language) is not var (share, _)
            || share < LessonScriptValidator.MinScriptShare)
        {
            return Refused("fallback:script", $"{language}: too few letters in the language's own script");
        }

        var extra = all.SelectMany(t => Digits().Matches(t)).Select(m => m.Value).Where(n => !allowed.Contains(n)).Distinct().ToList();
        return extra.Count > 0
            ? Refused("fallback:numbers", $"{language}: numbers not allowed: {string.Join(", ", extra)}")
            : null;
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

    private sealed record Item(string? Language, string? Concept, List<string>? Activity, string? Note);
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

    /// <param name="languages">The languages to write; with <paramref name="teacher"/>, the other languages only (not Kannada).</param>
    /// <param name="names">The class's child names, only to refuse output that contains one; never sent.</param>
    /// <param name="teacher">Her edited Kannada (D-92): A9 says the same in <paramref name="languages"/>. Checked for names before.</param>
    public async Task<TodaysWriteResult> WriteAsync(
        IReadOnlyList<string> languages,
        IReadOnlyList<Competency> lessons,
        TodaysPicture picture,
        IReadOnlyList<string>? feedback,
        IEnumerable<string> names,
        CancellationToken cancellationToken,
        TeacherKannada? teacher = null)
    {
        var request = new LlmRequest(
            prompts.Get(PromptId), UserContent(languages, lessons, picture, feedback, teacher), TodaysMessageValidator.Schema(), MaxTokens: 8192);
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

        var result = TodaysMessageValidator.Validate(response.Json, languages, lessons, picture, names, expectNote: teacher?.Note is not null);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, result.Verdict, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Curriculum data, code-chosen numbers and, when she edited it, her Kannada (already checked for names): no names, no
    /// child or class IDs (guardrail 1).
    /// </summary>
    public static string UserContent(
        IReadOnlyList<string> languages,
        IReadOnlyList<Competency> lessons,
        TodaysPicture picture,
        IReadOnlyList<string>? feedback,
        TeacherKannada? teacher = null) =>
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
            teacherKannada = teacher is null ? null : new { teacher.Concept, activity = teacher.Steps, teacher.Note },
            previousAttemptProblems = feedback ?? [],
        }, Json);
}
