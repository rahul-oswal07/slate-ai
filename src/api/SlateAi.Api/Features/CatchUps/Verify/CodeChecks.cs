using System.Globalization;
using System.Text.RegularExpressions;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Script;

namespace SlateAi.Api.Features.CatchUps.Verify;

public sealed class VerifierOptions
{
    public const string Section = "Verifier";

    /// <summary>Spoken-length limit for the script, before audio exists (spec §5.4 check 3).</summary>
    public int MaxScriptSeconds { get; set; } = 90;

    /// <summary>
    /// Spoken words per second by language, measured on Bulbul v3 "priya" (T-11: kn 1.86, te 1.91, ta 1.96), rounded down
    /// so the estimate errs long. English (T-18) and Hindi (D-78) are not measured yet: 2.2 is a cautious guess for both (Hindi runs faster than the Dravidian languages, so this errs long). The real
    /// audio is checked after text-to-speech anyway.
    /// </summary>
    public Dictionary<string, double> WordsPerSecond { get; set; } =
        new() { ["kn"] = 1.8, ["te"] = 1.85, ["ta"] = 1.9, ["en"] = 2.2, ["hi"] = 2.2 };

    /// <summary>Real audio limit, all segments together (FR-09: ≤ 95 s), checked after text-to-speech.</summary>
    public int MaxAudioSeconds { get; set; } = 95;

    /// <summary>Real audio size limit, all segments together (FR-09: ≤ 300 KB).</summary>
    public int MaxAudioKb { get; set; } = 300;

    /// <summary>Share of the script's letters that must be in the family's script, Kannada key words aside.</summary>
    public double MinScriptShare { get; set; } = 0.8;
}

/// <summary>One verifier check. <see cref="Detail"/> is shown to the teacher on "Needs review" (T3), with numbers.</summary>
public sealed record VerifierCheck(string Name, bool Passed, string Detail);

/// <summary>Everything the verifier looks at for one catch-up.</summary>
public sealed record VerifyInput(
    string Language,
    IReadOnlyList<string> TargetIds,
    ScriptContent Content,
    ActivityData Activity,
    CheckData Check,
    IReadOnlyList<string> Names);

/// <summary>
/// A4's code checks (spec §5.4), run before the LLM check. Pure and deterministic: numbers recomputed, script block,
/// spoken length, key words, objects, names.
/// </summary>
public static partial class CodeChecks
{
    public const string Numbers = "numbers";
    public const string ScriptBlock = "script";
    public const string Length = "length";
    public const string KeyWords = "keyWords";
    public const string Objects = "objects";
    public const string NamesCheck = "names";

    private static readonly Dictionary<string, (int From, int To, string Name)> Blocks = new()
    {
        ["kn"] = (0x0C80, 0x0CFF, "Kannada"),
        ["te"] = (0x0C00, 0x0C7F, "Telugu"),
        ["ta"] = (0x0B80, 0x0BFF, "Tamil"),
        ["hi"] = (0x0900, 0x097F, "Hindi"),

        // A–Z and a–z; the symbols between them aren't letters, so the letter filter drops them.
        ["en"] = (0x0041, 0x007A, "English"),
    };

    public static IReadOnlyList<VerifierCheck> Run(VerifyInput input, CurriculumGraph curriculum, VerifierOptions options) =>
    [
        NumbersCheck(input),
        Script(input, options),
        SpokenLength(input, options),
        Words(input, curriculum),
        ObjectsCheck(input),
        NamesAndPlaceholders(input),
    ];

    private static VerifierCheck NumbersCheck(VerifyInput input)
    {
        var check = input.Check;
        if (!input.TargetIds.Contains(check.CompetencyId))
        {
            return Fail(Numbers, $"the question tests {check.CompetencyId}, which is not a target");
        }

        if (Recompute(check.Kind, check.Question) is not { } expected)
        {
            return Fail(Numbers, $"the question \"{check.Question}\" can't be worked out");
        }

        if (check.CorrectIndex < 0 || check.CorrectIndex >= check.Options.Count || check.Options[check.CorrectIndex] != expected
            || check.Answer != expected)
        {
            return Fail(Numbers, $"the marked answer is not {expected}");
        }

        if (check.Options.Count(o => o == expected) != 1)
        {
            return Fail(Numbers, $"more than one picture shows {expected}");
        }

        var c = input.Content;
        foreach (var text in new[] { c.Script, c.GlossKn, c.GlossEn, c.ActivityInstruction, c.CheckPrompt })
        {
            foreach (Match m in Equation().Matches(text))
            {
                // The whole chain, e.g. "3 + 1 + 1 = 5" for a tens column with a carry, worked left to right.
                var terms = Term().Matches(m.Groups["sum"].Value);
                long right = 0;
                foreach (Match t in terms)
                {
                    var value = long.Parse(t.Groups["n"].Value);
                    right = t.Groups["op"].Value is "" or "+" ? right + value : right - value;
                }

                var said = long.Parse(m.Groups["said"].Value);
                if (right != said)
                {
                    var written = string.Join(" ", terms.Select((t, i) =>
                        i == 0 ? t.Groups["n"].Value : $"{(t.Groups["op"].Value == "+" ? "+" : "−")} {t.Groups["n"].Value}"));
                    return Fail(Numbers, $"says {written} = {said}; it is {right}");
                }
            }
        }

        var missing = input.Activity.Numbers.Where(n => !Regex.IsMatch(c.Script, $"(?<!\\d){n}(?!\\d)")).ToList();
        return missing.Count > 0
            ? Fail(Numbers, $"the script doesn't use the activity's number {string.Join(", ", missing)}")
            : Pass(Numbers, "sums and the picture question are correct");
    }

    private static VerifierCheck Script(VerifyInput input, VerifierOptions options)
    {
        if (!Blocks.TryGetValue(input.Language, out var block))
        {
            return Fail(ScriptBlock, $"unknown language {input.Language}");
        }

        var kannada = Blocks["kn"];
        var letters = input.Content.Script
            .Where(ch => char.IsLetter(ch) || CharUnicodeInfo.GetUnicodeCategory(ch) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
            .Where(ch => input.Language == "kn" || ch < kannada.From || ch > kannada.To)
            .ToList();
        var share = letters.Count == 0 ? 0 : letters.Count(ch => ch >= block.From && ch <= block.To) / (double)letters.Count;
        var percent = (int)Math.Floor(share * 100);
        return share >= options.MinScriptShare
            ? Pass(ScriptBlock, $"{percent}% in {block.Name} letters")
            : Fail(ScriptBlock, $"only {percent}% in {block.Name} letters, needs {options.MinScriptShare:P0}");
    }

    private static VerifierCheck SpokenLength(VerifyInput input, VerifierOptions options)
    {
        var words = input.Content.Script.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var rate = options.WordsPerSecond.GetValueOrDefault(input.Language, 1.8);
        var seconds = (int)Math.Ceiling(words / rate);
        return seconds <= options.MaxScriptSeconds
            ? Pass(Length, $"about {seconds} seconds, limit {options.MaxScriptSeconds}")
            : Fail(Length, $"about {seconds} seconds, limit {options.MaxScriptSeconds}");
    }

    private static VerifierCheck Words(VerifyInput input, CurriculumGraph curriculum)
    {
        var choices = input.TargetIds.SelectMany(id => curriculum.Get(id)?.KeyWordsKn ?? []).ToHashSet(StringComparer.Ordinal);
        var words = input.Content.KeyWordsKn;
        if (words.Count != A3Validator.KeyWords || words.Any(w => !choices.Contains(w)))
        {
            return Fail(KeyWords, "the key words are not the lesson's textbook words");
        }

        var rare = words.FirstOrDefault(w => Occurrences(input.Content.Script, w) < 2);
        return rare is null
            ? Pass(KeyWords, $"{string.Join(", ", words)} each said twice")
            : Fail(KeyWords, $"the key word {rare} is said {Occurrences(input.Content.Script, rare)} time(s), needs 2");
    }

    private static VerifierCheck ObjectsCheck(VerifyInput input)
    {
        var objects = input.Content.Objects;
        if (objects.Count == 0)
        {
            return Fail(Objects, "no household object suggested");
        }

        var other = objects.FirstOrDefault(o => !A3Validator.AllowedObjects.Contains(o));
        return other is null
            ? Pass(Objects, string.Join(", ", objects))
            : Fail(Objects, $"{other} is not on the allowed list");
    }

    private static VerifierCheck NamesAndPlaceholders(VerifyInput input)
    {
        var c = input.Content;
        return A3Validator.UnsafeText([c.Script, c.ActivityInstruction, c.ObjectOption, c.CheckPrompt, c.GlossKn, c.GlossEn], input.Names) switch
        {
            "fallback:placeholder" => Fail(NamesCheck, "has a placeholder or markup"),
            "fallback:name" => Fail(NamesCheck, "has a name from the class"),
            _ => Pass(NamesCheck, "no names"),
        };
    }

    /// <summary>The answer to a code-made question, from its kind and display text (ProblemGenerator's formats).</summary>
    public static int? Recompute(string kind, string question)
    {
        try
        {
            switch (kind)
            {
                case "add":
                    return question.Split(" + ").Sum(int.Parse);
                case "sub":
                    var parts = question.Split(" − ");
                    return int.Parse(parts[0]) - int.Parse(parts[1]);
                case "expanded":
                    return question.Split(" + ").Sum(int.Parse);
                case "count":
                    return int.Parse(question);
                case "placeValue":
                    var p = question.Split(" in ");
                    var (digit, number) = (p[0], p[1]);
                    var index = number.IndexOf(digit, StringComparison.Ordinal);
                    return index < 0 ? null : int.Parse(digit) * (int)Math.Pow(10, number.Length - 1 - index);
                case "biggest":
                    return question.Split(" · ").Max(int.Parse);
                case "smallest":
                    return question.Split(" · ").Min(int.Parse);
                default:
                    return null;
            }
        }
        catch (FormatException)
        {
            return null;
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static int Occurrences(string text, string word)
    {
        var count = 0;
        for (var at = text.IndexOf(word, StringComparison.Ordinal); at >= 0; at = text.IndexOf(word, at + word.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static VerifierCheck Pass(string name, string detail) => new(name, true, detail);

    private static VerifierCheck Fail(string name, string detail) => new(name, false, detail);

    // "11 − 5 = 6", "3 + 1 + 1 = 5": two or more terms; hyphen, minus sign or en dash; Latin digits only (textbook
    // numerals). The look-behind stops a match starting in the middle of a longer chain or number.
    [GeneratedRegex(@"(?<![\d+\-−–]\s*)(?<sum>\d+(?:\s*[+\-−–]\s*\d+)+)\s*=\s*(?<said>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex Equation();

    [GeneratedRegex(@"(?<op>[+\-−–]?)\s*(?<n>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex Term();
}
