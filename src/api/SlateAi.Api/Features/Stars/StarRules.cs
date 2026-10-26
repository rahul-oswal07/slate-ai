using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Features.Stars;

/// <param name="Finished">The week is over: its Saturday is before today, or today is Saturday and the register is in.</param>
/// <param name="SchoolDays">Days in the week that have a register.</param>
/// <param name="Children">Earned the week's star (finished) or on track for it (current), in roll-number order. Never the others.</param>
public sealed record WeekStars(bool Finished, int SchoolDays, IReadOnlyList<StarChild> Children);

/// <param name="Attendance">Weekly stars this term, finished weeks only, up to the week shown.</param>
/// <param name="CatchUps">Catch-ups answered at home, up to the week shown.</param>
public sealed record StarChild(Guid ChildId, int RollNo, string FirstName, int Attendance, int CatchUps)
{
    public int Total => Attendance + CatchUps;
}

/// <summary>
/// Attendance stars (FR-24, D-81): a star for each finished school week with at most
/// <see cref="StarsOptions.MaxDaysAwayPerWeek"/> days away and at least one present, and one for each catch-up answered
/// at home whatever the answer. Counted in code from the register and the catch-ups; nothing is stored, so stars
/// can't go away. Pure: <c>today</c> is passed in.
/// </summary>
public static class StarRules
{
    /// <summary>Null when the week has no register at all.</summary>
    public static WeekStars? For(LedgerData data, DateOnly weekOf, DateOnly today, StarsOptions options)
    {
        var monday = CompressionPlanner.Week(weekOf).From;
        var saturday = monday.AddDays(5);

        var schoolDays = data.Register.Where(r => r.Date >= monday && r.Date <= saturday).Select(r => r.Date).Distinct().Count();
        if (schoolDays == 0)
        {
            return null;
        }

        var finished = saturday < today || (today == saturday && data.Register.Any(r => r.Date == saturday));

        // Weeks that count towards the term total: every earlier week, and this one once it is over.
        var lastCounted = finished ? monday : monday.AddDays(-7);
        var weeksByChild = data.Register
            .Where(r => r.Date.DayOfWeek != DayOfWeek.Sunday)
            .GroupBy(r => r.ChildId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => CompressionPlanner.Week(r.Date).From).ToDictionary(w => w.Key, w => Earned(w, options)));

        var catchUps = (data.CatchUps ?? [])
            .Where(c => c.Outcome is not null && c.WeekOf <= saturday)
            .GroupBy(c => c.ChildId)
            .ToDictionary(g => g.Key, g => g.Count());

        var children = data.Children
            .Where(c => weeksByChild.TryGetValue(c.Id, out var weeks) && weeks.GetValueOrDefault(monday))
            .OrderBy(c => c.RollNo)
            .Select(c => new StarChild(
                c.Id,
                c.RollNo,
                c.FirstName,
                weeksByChild[c.Id].Count(w => w.Value && w.Key <= lastCounted),
                catchUps.GetValueOrDefault(c.Id)))
            .ToList();

        return new WeekStars(finished, schoolDays, children);
    }

    /// <summary>One child's rows for one week: present at least once and away at most the limit.</summary>
    private static bool Earned(IEnumerable<RegisterRow> rows, StarsOptions options)
    {
        var present = 0;
        var away = 0;
        foreach (var row in rows)
        {
            if (row.Present)
            {
                present++;
            }
            else
            {
                away++;
            }
        }

        return present >= 1 && away <= options.MaxDaysAwayPerWeek;
    }
}
