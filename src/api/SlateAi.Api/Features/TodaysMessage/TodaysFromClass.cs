using System.Text.RegularExpressions;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>What she said in class, as A9 may see it (T-118): A1's summary and the examples she used.</summary>
public sealed record TodaysFromClass(string? SummaryKn, IReadOnlyList<string> Examples);

/// <summary>
/// Picks what of the day's confirmed lesson may reach A9 (T-118). Pure. Only that day's lesson for these lessons; a phrase
/// with a placeholder, brace, sign or child's name is dropped (guardrail 1), and so is one with a number A9 may not write
/// (guardrail 4: numbers still come only from the book), so her examples shape the wording, never the numbers.
/// </summary>
public static partial class TodaysFromClassFilter
{
    public const int MaxExamples = 5;
    public const int MaxChars = 120;

    /// <param name="allowed">The numbers A9 may write (<see cref="TodaysMessageValidator.AllowedNumbers"/>).</param>
    /// <param name="names">The class's child names, only to drop a phrase that holds one; never sent.</param>
    /// <returns>Null when nothing is left, so the request says nothing about her lesson.</returns>
    public static TodaysFromClass? From(
        ConfirmedLesson? lesson,
        DateOnly date,
        IReadOnlyList<string> competencyIds,
        IReadOnlySet<string> allowed,
        IReadOnlyList<string> names)
    {
        if (lesson is null || lesson.Date != date || !lesson.CompetencyIds.Intersect(competencyIds).Any())
        {
            return null;
        }

        bool Safe(string text) =>
            text.Length is > 0 and <= MaxChars
            && A3Validator.UnsafeText([text], names) is null
            && Digits().Matches(text).All(m => allowed.Contains(m.Value));

        var summary = lesson.SummaryKn?.Trim() is { } s && Safe(s) ? s : null;
        var examples = (lesson.ExamplesUsed ?? [])
            .Select(e => e.Trim())
            .Where(Safe)
            .Distinct()
            .Take(MaxExamples)
            .ToList();
        return summary is null && examples.Count == 0 ? null : new TodaysFromClass(summary, examples);
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();
}
