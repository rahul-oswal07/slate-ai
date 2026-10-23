namespace SlateAi.Api.Features.Ledger;

/// <summary>A run of consecutive absent school days. <see cref="ReturnedOn"/> is null while the child is still away.</summary>
public sealed record AbsenceSpell(DateOnly From, DateOnly To, int SchoolDays, DateOnly? ReturnedOn);

/// <summary>How quickly absent children came back (D-20/D-24). Counts only; computed in code, never by the LLM.</summary>
public sealed record ReturnSpeed(
    DateOnly From,
    DateOnly To,
    int OneDay,
    int TwoDays,
    int ThreeDays,
    int FourPlusDays,
    int BackWithinTwoDays,
    int Total);

public static class AbsenceSpells
{
    /// <summary>
    /// Groups one child's register into absence spells. Only dates with a register entry are school days,
    /// so a holiday between two absent days neither breaks nor lengthens the spell.
    /// </summary>
    public static IReadOnlyList<AbsenceSpell> For(IEnumerable<(DateOnly Date, bool Present)> register)
    {
        var spells = new List<AbsenceSpell>();
        DateOnly? from = null;
        DateOnly to = default;
        var days = 0;

        foreach (var (date, present) in register.OrderBy(r => r.Date))
        {
            if (!present)
            {
                from ??= date;
                to = date;
                days++;
            }
            else if (from is not null)
            {
                spells.Add(new AbsenceSpell(from.Value, to, days, date));
                from = null;
                days = 0;
            }
        }

        if (from is not null)
        {
            spells.Add(new AbsenceSpell(from.Value, to, days, null));
        }

        return spells;
    }

    /// <summary>Consecutive absent school days up to the latest register entry; 0 if the child is back.</summary>
    public static int CurrentDays(IReadOnlyList<AbsenceSpell> spells) =>
        spells.Count > 0 && spells[^1].ReturnedOn is null ? spells[^1].SchoolDays : 0;

    /// <summary>Summarises spells that started in the 7 calendar days ending on <paramref name="asOf"/>.</summary>
    public static ReturnSpeed Summarise(IEnumerable<AbsenceSpell> spells, DateOnly asOf) =>
        Summarise(spells, asOf.AddDays(-6), asOf);

    /// <summary>
    /// Summarises the spells that <b>started</b> between <paramref name="from"/> and <paramref name="to"/> (T6 uses a
    /// school week, D-24). A child still away counts in the total by the days so far and is never "back within two
    /// school days" until they return. All numbers come from the same list, so they always add up (B-09).
    /// </summary>
    public static ReturnSpeed Summarise(IEnumerable<AbsenceSpell> spells, DateOnly from, DateOnly to)
    {
        var inWindow = spells.Where(s => s.From >= from && s.From <= to).ToList();
        return new ReturnSpeed(
            from,
            to,
            OneDay: inWindow.Count(s => s.SchoolDays == 1),
            TwoDays: inWindow.Count(s => s.SchoolDays == 2),
            ThreeDays: inWindow.Count(s => s.SchoolDays == 3),
            FourPlusDays: inWindow.Count(s => s.SchoolDays >= 4),
            BackWithinTwoDays: inWindow.Count(s => s.ReturnedOn is not null && s.SchoolDays <= 2),
            Total: inWindow.Count);
    }
}
