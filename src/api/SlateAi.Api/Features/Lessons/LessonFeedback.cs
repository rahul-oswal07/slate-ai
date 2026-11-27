namespace SlateAi.Api.Features.Lessons;

/// <summary>
/// What A1 offered for her note and what she confirmed (T-102): kept as an eval case for A1. Only curriculum IDs,
/// A1's confidence and A1's own (name-scrubbed) summary and examples, which the day's lesson already stores. Never a
/// name, the transcript or the audio: none of them is stored anywhere.
/// </summary>
/// <param name="MappingStartedAt">Identifies the note the match came from (the lesson row's <c>MappingStartedAt</c>).</param>
/// <param name="MatchStatus">"mapped" (she saw the lessons ticked) or "low" (she was asked to choose between two).</param>
/// <param name="MatchedCompetencyIds">A1's ranked match, as it was before she confirmed.</param>
/// <param name="ChosenCompetencyIds">What she confirmed, in book order.</param>
/// <param name="ConfirmedVia">"voice": from the matched list on T2; "picked": from the lesson list instead.</param>
/// <param name="Outcome">accepted, edited or replaced (<see cref="LessonFeedbackBuilder.Outcome"/>).</param>
public sealed record LessonFeedbackRecord(
    DateOnly Date,
    DateTimeOffset MappingStartedAt,
    int PromptVersion,
    string MatchStatus,
    IReadOnlyList<string> MatchedCompetencyIds,
    double? Confidence,
    IReadOnlyList<string> ChosenCompetencyIds,
    string ConfirmedVia,
    string Outcome,
    string? SummaryKn,
    string? SummaryEn,
    IReadOnlyList<string> ExamplesUsed);

/// <summary>A saved <see cref="LessonFeedbackRecord"/>.</summary>
public sealed record SavedLessonFeedback(Guid Id, Guid ClassId, DateTimeOffset CreatedAt, LessonFeedbackRecord Feedback);

/// <summary>Builds the eval case saved when she confirms a lesson A1 matched (T-102). Pure, so it is unit-tested.</summary>
public static class LessonFeedbackBuilder
{
    /// <summary>She confirmed what T2 offered: every matched lesson, or one of the two shown for a low match.</summary>
    public const string Accepted = "accepted";

    /// <summary>She kept some of A1's lessons and unticked or added others.</summary>
    public const string Edited = "edited";

    /// <summary>None of A1's lessons: she picked a different one.</summary>
    public const string Replaced = "replaced";

    /// <summary>How many lessons T2 shows as choices for a low match (ConfirmLessonScreen).</summary>
    public const int LowChoices = 2;

    /// <summary>
    /// The case for confirming <paramref name="chosen"/> over the day's lesson as it is now, or null when there is no A1
    /// match to compare with (picked with no note, a note that couldn't be heard, still processing, already confirmed).
    /// </summary>
    public static LessonFeedbackRecord? From(
        DateOnly date, LessonRecord? current, IReadOnlyList<string> chosen, string confirmedVia, int promptVersion)
    {
        if (current is not { Status: LessonValues.Mapped or LessonValues.Low, MappingStartedAt: { } startedAt }
            || current.CompetencyIds.Count == 0)
        {
            return null;
        }

        return new LessonFeedbackRecord(
            date,
            startedAt,
            promptVersion,
            current.Status,
            current.CompetencyIds,
            current.Confidence,
            chosen,
            confirmedVia,
            Outcome(current.Status, current.CompetencyIds, chosen),
            current.SummaryKn,
            current.SummaryEn,
            current.ExamplesUsed ?? []);
    }

    /// <summary>
    /// Compared with what T2 offered her (every matched lesson ticked; for a low match, the first two to choose from):
    /// accepted when she confirmed exactly that (for a low match, one of the two), edited when she kept some of it and
    /// unticked or added others, replaced when she kept none of it.
    /// </summary>
    public static string Outcome(string matchStatus, IReadOnlyList<string> matched, IReadOnlyList<string> chosen)
    {
        var low = matchStatus == LessonValues.Low;
        var offered = (low ? matched.Take(LowChoices) : matched).ToHashSet(StringComparer.Ordinal);
        if (low ? chosen.Count == 1 && offered.Contains(chosen[0]) : offered.SetEquals(chosen))
        {
            return Accepted;
        }

        return chosen.Any(offered.Contains) ? Edited : Replaced;
    }
}
