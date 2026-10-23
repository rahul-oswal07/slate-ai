using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Parent;

namespace SlateAi.Api.Features.Week;

/// <summary>Reasons the families gave (FR-21): one per absent child, counts only, never names.</summary>
public sealed record WeekReasons(int Unwell, int TravelOrWork, int Festival, int NeededAtHome, int Other, int NotGiven);

/// <summary>
/// The pacing nudge (FR-13, D-60): the lesson most children missed this week, when 5 or more did. Every number is
/// counted here; the screen fills a fixed sentence with them (no AI).
/// </summary>
/// <param name="NextCompetencyId">The next lesson in the book ("before word problems"); null at the end of the book.</param>
public sealed record PacingNudge(
    Guid LessonId,
    DateOnly Date,
    IReadOnlyList<string> CompetencyIds,
    int N,
    int Total,
    int Minutes,
    string? NextCompetencyId);

/// <summary>T6 for one school week (Mon–Sat).</summary>
/// <param name="Empty">No absence started this week: "No absences this week", no comparison (D-61).</param>
/// <param name="Decision">"recap" or "skip" for this week's nudge, or null.</param>
public sealed record WeekResponse(
    DateOnly From,
    DateOnly To,
    bool Empty,
    ReturnSpeed ThisWeek,
    ReturnSpeed LastWeek,
    WeekReasons Reasons,
    PacingNudge? Nudge,
    string? Decision);

/// <summary>The stored answer to a week's nudge.</summary>
public sealed record DecisionRow(Guid LessonId, string Decision);

/// <summary>
/// Builds the week from attendance, confirmed lessons and reasons. Pure and deterministic: no AI. A spell belongs to
/// the week it started in; "back within 2 school days" means returned after at most 2 (Rahul, 23 Oct).
/// </summary>
public static class WeekBuilder
{
    /// <summary>D-60: the nudge shows when this many children missed the same lesson.</summary>
    public const int NudgeThreshold = 5;

    public const int RecapMinutes = 10;

    public static WeekResponse Build(LedgerData data, CurriculumGraph curriculum, DateOnly weekOf, DecisionRow? decision)
    {
        var monday = CompressionPlanner.Week(weekOf).From;
        var saturday = monday.AddDays(5);
        bool InWeek(DateOnly d) => d >= monday && d <= saturday;

        var spells = data.Register
            .GroupBy(r => r.ChildId)
            .SelectMany(g => AbsenceSpells.For(g.Select(r => (r.Date, r.Present))))
            .ToList();
        var thisWeek = AbsenceSpells.Summarise(spells, monday, saturday);
        var lastWeek = AbsenceSpells.Summarise(spells, monday.AddDays(-7), saturday.AddDays(-7));

        var nudge = Nudge(data, curriculum, InWeek, saturday);
        return new WeekResponse(
            monday,
            saturday,
            thisWeek.Total == 0,
            thisWeek,
            lastWeek,
            Reasons(data, InWeek),
            nudge,
            nudge is not null && decision?.LessonId == nudge.LessonId ? decision.Decision : null);
    }

    /// <summary>
    /// Each child absent on any day this week counts once: their latest reason this week, else "not given". So the
    /// counts always add up to the absent children (T6 §7).
    /// </summary>
    private static WeekReasons Reasons(LedgerData data, Func<DateOnly, bool> inWeek)
    {
        var absent = data.Register.Where(r => !r.Present && inWeek(r.Date)).Select(r => r.ChildId).Distinct().ToList();
        var latest = (data.Reasons ?? [])
            .Where(r => inWeek(r.Date))
            .GroupBy(r => r.ChildId)
            .ToDictionary(g => g.Key, g => g.MaxBy(r => r.Date)!.Category);
        var categories = absent.Select(id => latest.GetValueOrDefault(id)).ToList();
        int Count(string category) => categories.Count(c => c == category);
        return new WeekReasons(
            Count(ReasonCategory.Unwell),
            Count(ReasonCategory.TravelOrWork),
            Count(ReasonCategory.Festival),
            Count(ReasonCategory.NeededAtHome),
            Count(ReasonCategory.Other),
            categories.Count(c => c is null));
    }

    /// <summary>The confirmed lesson this week with the most children absent that day, if at least 5 (ties: latest).</summary>
    private static PacingNudge? Nudge(LedgerData data, CurriculumGraph curriculum, Func<DateOnly, bool> inWeek, DateOnly saturday)
    {
        var absentOn = data.Register.Where(r => !r.Present).ToLookup(r => r.Date, r => r.ChildId);
        var best = data.Lessons
            .Where(l => inWeek(l.Date))
            .Select(l => (Lesson: l, Missed: absentOn[l.Date].Distinct().Count()))
            .OrderByDescending(x => x.Missed)
            .ThenByDescending(x => x.Lesson.Date)
            .FirstOrDefault();
        if (best.Lesson is null || best.Missed < NudgeThreshold)
        {
            return null;
        }

        // "Before {next}": the next lesson in the book after the furthest one she has taught (as A2 plans next week).
        var latestTaught = data.Lessons
            .Where(l => l.Date <= saturday)
            .SelectMany(l => l.CompetencyIds)
            .Where(curriculum.Contains)
            .MaxBy(id => curriculum.Get(id)!.Sequence);
        return new PacingNudge(
            best.Lesson.Id,
            best.Lesson.Date,
            best.Lesson.CompetencyIds,
            best.Missed,
            data.Children.Count,
            RecapMinutes,
            curriculum.NextInBook(latestTaught)?.Id);
    }
}
