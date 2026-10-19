using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Tests.Features.Ledger;

public sealed class AbsenceSpellsTests
{
    // Mon 12 Oct 2026; tests describe registers relative to it.
    private static readonly DateOnly Mon = new(2026, 10, 12);

    private static (DateOnly, bool)[] Register(params (int Day, bool Present)[] days) =>
        days.Select(d => (Mon.AddDays(d.Day), d.Present)).ToArray();

    [Fact]
    public void Always_present_has_no_spells()
    {
        var spells = AbsenceSpells.For(Register((0, true), (1, true), (2, true)));

        Assert.Empty(spells);
        Assert.Equal(0, AbsenceSpells.CurrentDays(spells));
    }

    [Fact]
    public void Single_absent_day_then_back()
    {
        var spells = AbsenceSpells.For(Register((0, true), (1, false), (2, true)));

        Assert.Equal([new AbsenceSpell(Mon.AddDays(1), Mon.AddDays(1), 1, Mon.AddDays(2))], spells);
    }

    [Fact]
    public void Three_day_spell_records_the_return_day()
    {
        var spells = AbsenceSpells.For(Register((0, false), (1, false), (2, false), (3, true)));

        var spell = Assert.Single(spells);
        Assert.Equal(3, spell.SchoolDays);
        Assert.Equal(Mon.AddDays(3), spell.ReturnedOn);
    }

    [Fact]
    public void Ongoing_spell_has_no_return_day_and_counts_as_current()
    {
        var spells = AbsenceSpells.For(Register((0, true), (1, false), (2, false)));

        var spell = Assert.Single(spells);
        Assert.Null(spell.ReturnedOn);
        Assert.Equal(2, AbsenceSpells.CurrentDays(spells));
    }

    [Fact]
    public void Holiday_inside_a_spell_neither_breaks_nor_lengthens_it()
    {
        // Fri absent, weekend has no register, Mon absent, Tue back.
        var spells = AbsenceSpells.For(Register((4, false), (7, false), (8, true)));

        var spell = Assert.Single(spells);
        Assert.Equal(2, spell.SchoolDays);
        Assert.Equal(Mon.AddDays(4), spell.From);
        Assert.Equal(Mon.AddDays(7), spell.To);
    }

    [Fact]
    public void Separate_spells_are_kept_apart_and_input_order_does_not_matter()
    {
        var spells = AbsenceSpells.For(Register((3, false), (0, false), (2, true), (1, true), (4, true)));

        Assert.Equal(2, spells.Count);
        Assert.All(spells, s => Assert.Equal(1, s.SchoolDays));
        Assert.Equal(0, AbsenceSpells.CurrentDays(spells));
    }

    [Fact]
    public void Summary_buckets_spells_by_length_and_counts_quick_returns()
    {
        var asOf = Mon.AddDays(6);
        AbsenceSpell[] spells =
        [
            new(Mon, Mon, 1, Mon.AddDays(1)),                          // 1 day, back
            new(Mon.AddDays(1), Mon.AddDays(2), 2, Mon.AddDays(3)),    // 2 days, back
            new(Mon.AddDays(2), Mon.AddDays(5), 4, null),              // 4+, still away
            new(Mon.AddDays(5), Mon.AddDays(6), 2, null),              // 2 days, still away: not "back"
            new(Mon.AddDays(-1), Mon.AddDays(1), 3, Mon.AddDays(2)),   // started before the window
        ];

        var summary = AbsenceSpells.Summarise(spells, asOf);

        Assert.Equal(new ReturnSpeed(Mon, asOf, 1, 2, 0, 1, BackWithinTwoDays: 2, Total: 4), summary);
    }
}
