using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Features.Stars;

/// <param name="Finished">The week is over: its Saturday is before today, or today is Saturday and the register is in.</param>
/// <param name="SchoolDays">Days in the week that have a register.</param>
/// <param name="Children">Earned the week's star (finished) or on track for it (current), in roll-number order. Never the others.</param>
public sealed record WeekStars(bool Finished, int SchoolDays, IReadOnlyList<StarChild> Children);

/// <summary>A child with the week's star. No counts: each week stands alone (D-118).</summary>
public sealed record StarChild(Guid ChildId, int RollNo, string FirstName);

/// <summary>
/// The weekly star (FR-24, D-118, replacing D-81's rule): a child present on <b>every</b> school day of the week earns
/// it. A school day is a day of the week (Mon–Sat) with a register; a day without one doesn't count. Each week is a
/// fresh list: nothing is added up or carried forward, and catch-ups answered at home add nothing. Counted in code from
/// the register; nothing is stored. Pure: <c>today</c> is passed in.
/// </summary>
public static class StarRules
{
    /// <summary>Null when the week has no register at all.</summary>
    public static WeekStars? For(LedgerData data, DateOnly weekOf, DateOnly today)
    {
        var monday = CompressionPlanner.Week(weekOf).From;
        var saturday = monday.AddDays(5);

        var week = data.Register.Where(r => r.Date >= monday && r.Date <= saturday).ToList();
        var schoolDays = week.Select(r => r.Date).Distinct().ToList();
        if (schoolDays.Count == 0)
        {
            return null;
        }

        var finished = saturday < today || (today == saturday && schoolDays.Contains(saturday));

        // Present on every school day so far: a day she was away, or a day the register has no row for her, loses it.
        var presentDays = week
            .Where(r => r.Present)
            .GroupBy(r => r.ChildId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Date).Distinct().Count());
        var awayAny = week.Where(r => !r.Present).Select(r => r.ChildId).ToHashSet();

        var children = data.Children
            .Where(c => !awayAny.Contains(c.Id) && presentDays.GetValueOrDefault(c.Id) == schoolDays.Count)
            .OrderBy(c => c.RollNo)
            .Select(c => new StarChild(c.Id, c.RollNo, c.FirstName))
            .ToList();

        return new WeekStars(finished, schoolDays.Count, children);
    }
}
