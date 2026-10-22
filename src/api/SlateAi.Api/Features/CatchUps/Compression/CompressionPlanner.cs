using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Features.CatchUps.Compression;

/// <summary>A lesson the child missed this week and the days it was taught.</summary>
public sealed record MissedCompetency(string Id, IReadOnlyList<DateOnly> Dates);

/// <summary>
/// What A2 decides from, for one child. IDs only, never names (guardrail 1).
/// </summary>
/// <param name="Missed">This week's missed lessons, in the order they were taught.</param>
/// <param name="NextWeek">The lessons coming next, in book order.</param>
/// <param name="Candidates">missed ∩ ancestors(next week), nearest first (code, never the LLM).</param>
public sealed record CompressionInput(
    Guid ChildId,
    IReadOnlyList<MissedCompetency> Missed,
    IReadOnlyList<string> NextWeek,
    IReadOnlyList<string> Candidates);

/// <summary>
/// Builds A2's inputs from the ledger and the curriculum (FR-06). Pure and deterministic: the week window, the
/// missed lessons, next week's lessons and the candidate set are all computed here, never by the LLM.
/// </summary>
public static class CompressionPlanner
{
    /// <summary>How many lessons count as "next week": one per school day.</summary>
    public const int NextWeekLessons = 5;

    /// <summary>The school week up to <paramref name="date"/>: Monday to that day.</summary>
    public static (DateOnly From, DateOnly To) Week(DateOnly date) =>
        (date.AddDays(-(((int)date.DayOfWeek + 6) % 7)), date);

    /// <summary>
    /// Up to <see cref="NextWeekLessons"/> lessons after the latest one taught, in book order. There is no syllabus
    /// data yet, so the book order stands in for the plan; fewer near the end of the book.
    /// </summary>
    public static IReadOnlyList<string> NextWeek(CurriculumGraph curriculum, IEnumerable<string> latestTaught)
    {
        var last = latestTaught.Where(curriculum.Contains).MaxBy(id => curriculum.Get(id)!.Sequence);
        var next = new List<string>();
        for (var c = curriculum.NextInBook(last); c is not null && next.Count < NextWeekLessons; c = curriculum.NextInBook(c.Id))
        {
            next.Add(c.Id);
        }

        return next;
    }

    /// <summary>One input per child who missed a confirmed lesson in the week up to <paramref name="date"/>.</summary>
    public static IReadOnlyList<CompressionInput> Plan(
        LedgerResponse ledger, IReadOnlyList<LessonRow> lessons, DateOnly date, CurriculumGraph curriculum)
    {
        var (from, to) = Week(date);
        var latest = lessons.Where(l => l.Date <= to).MaxBy(l => l.Date);
        var nextWeek = NextWeek(curriculum, latest?.CompetencyIds ?? []);

        var inputs = new List<CompressionInput>();
        foreach (var child in ledger.Children)
        {
            var missed = child.MissedLessons
                .Where(m => m.Date >= from && m.Date <= to)
                .OrderBy(m => m.Date)
                .SelectMany(m => m.CompetencyIds.Where(curriculum.Contains).Select(id => (Id: id, m.Date)))
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .Select(g => new MissedCompetency(g.Key, g.Select(x => x.Date).Distinct().ToList()))
                .ToList();
            if (missed.Count == 0)
            {
                continue;
            }

            inputs.Add(new CompressionInput(
                child.ChildId, missed, nextWeek, curriculum.CandidateSet(missed.Select(m => m.Id), nextWeek)));
        }

        return inputs;
    }
}
