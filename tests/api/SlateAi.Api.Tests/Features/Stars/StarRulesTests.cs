using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Stars;

namespace SlateAi.Api.Tests.Features.Stars;

public sealed class StarRulesTests
{
    // Weeks: A = Mon 5 Oct, B = Mon 12 Oct, C = Mon 19 Oct 2026 (Sat 24 Oct).
    private static readonly DateOnly WeekA = new(2026, 10, 5);
    private static readonly DateOnly WeekB = new(2026, 10, 12);
    private static readonly DateOnly WeekC = new(2026, 10, 19);
    private static readonly DateOnly Sunday = new(2026, 10, 25);
    private static readonly StarsOptions Default = new();

    private readonly LedgerChildRow _asha = new(Guid.NewGuid(), 3, "Asha");
    private readonly LedgerChildRow _ravi = new(Guid.NewGuid(), 4, "Ravi");
    private readonly LedgerChildRow _kiran = new(Guid.NewGuid(), 9, "Kiran");

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(6, false)]
    public void A_finished_week_earns_a_star_with_at_most_one_day_away(int daysAway, bool earned)
    {
        var data = Data([_asha], Week(WeekA, _asha, daysAway));

        var stars = StarRules.For(data, WeekA, Sunday, Default)!;

        Assert.True(stars.Finished);
        Assert.Equal(earned ? [_asha.Id] : [], stars.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void The_limit_is_an_option()
    {
        var data = Data([_asha], Week(WeekA, _asha, 1));

        Assert.Empty(StarRules.For(data, WeekA, Sunday, new StarsOptions { MaxDaysAwayPerWeek = 0 })!.Children);
        Assert.Single(StarRules.For(Data([_asha], Week(WeekA, _asha, 2)), WeekA, Sunday, new StarsOptions { MaxDaysAwayPerWeek = 2 })!.Children);
    }

    [Fact]
    public void A_holiday_week_counts_days_away_not_days_present_and_a_week_without_a_register_is_not_a_week()
    {
        // Three school days (Mon, Tue, Wed); Asha away once: a star. Nothing at all in week A.
        var data = Data([_asha], Days(WeekB, _asha, [true, false, true]));

        Assert.Null(StarRules.For(data, WeekA, Sunday, Default));
        var week = StarRules.For(data, WeekB, Sunday, Default)!;
        Assert.Equal(3, week.SchoolDays);
        Assert.Single(week.Children);
    }

    [Fact]
    public void Away_every_school_day_earns_nothing_even_in_a_one_day_week()
    {
        var data = Data([_asha, _ravi], Days(WeekB, _asha, [false]), Days(WeekB, _ravi, [true]));

        var stars = StarRules.For(data, WeekB, Sunday, Default)!;

        Assert.Equal([_ravi.Id], stars.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void A_child_with_no_rows_that_week_is_skipped()
    {
        var data = Data([_asha, _kiran], Week(WeekB, _asha, 0));

        Assert.Equal([_asha.Id], StarRules.For(data, WeekB, Sunday, Default)!.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void The_current_week_lists_children_on_track_and_adds_no_attendance_star_yet()
    {
        var wednesday = WeekC.AddDays(2);
        var data = Data(
            [_asha, _ravi],
            [.. Days(WeekB, _asha, [true, true, true]), .. Days(WeekC, _asha, [true, true, true]),
             .. Days(WeekC, _ravi, [false, false, true])]);

        var stars = StarRules.For(data, WeekC, wednesday, Default)!;

        Assert.False(stars.Finished);
        var only = Assert.Single(stars.Children);
        Assert.Equal(_asha.Id, only.ChildId);
        Assert.Equal(1, only.Attendance); // last week's star; this week's is not counted until it is over
    }

    [Fact]
    public void Saturday_is_finished_only_once_its_register_is_in()
    {
        var saturday = WeekC.AddDays(5);
        var without = Data([_asha], Days(WeekC, _asha, [true, true, true]));
        var with = Data([_asha], Days(WeekC, _asha, [true, true, true, true, true, true]));

        Assert.False(StarRules.For(without, WeekC, saturday, Default)!.Finished);
        Assert.True(StarRules.For(with, WeekC, saturday, Default)!.Finished);
    }

    [Fact]
    public void The_term_total_adds_finished_weeks_and_an_older_week_shows_the_total_as_it_was()
    {
        // Asha earns A and C, misses B (3 days away).
        var data = Data(
            [_asha],
            [.. Week(WeekA, _asha, 0), .. Week(WeekB, _asha, 3), .. Week(WeekC, _asha, 1)]);

        Assert.Equal(2, StarRules.For(data, WeekC, Sunday, Default)!.Children.Single().Attendance);
        Assert.Equal(1, StarRules.For(data, WeekA, Sunday, Default)!.Children.Single().Attendance);
    }

    [Fact]
    public void Answered_catch_ups_count_whatever_the_answer_and_later_weeks_do_not()
    {
        var data = Data(
            [_asha],
            [.. Week(WeekA, _asha, 0), .. Week(WeekB, _asha, 0)],
            [
                CatchUp(_asha, WeekA, "caughtUp"),
                CatchUp(_asha, WeekA, "needsBuddy"),
                CatchUp(_asha, WeekA, null),
                CatchUp(_asha, WeekB, "caughtUp"),
            ]);

        Assert.Equal(2, StarRules.For(data, WeekA, Sunday, Default)!.Children.Single().CatchUps);
        var later = StarRules.For(data, WeekB, Sunday, Default)!.Children.Single();
        Assert.Equal(3, later.CatchUps);
        Assert.Equal(later.Attendance + later.CatchUps, later.Total);
    }

    [Fact]
    public void Children_come_in_roll_order_and_those_without_a_star_are_never_listed()
    {
        var data = Data(
            [_kiran, _ravi, _asha],
            [.. Week(WeekB, _kiran, 0), .. Week(WeekB, _ravi, 4), .. Week(WeekB, _asha, 0)]);

        var stars = StarRules.For(data, WeekB, Sunday, Default)!;

        Assert.Equal([3, 9], stars.Children.Select(c => c.RollNo));
    }

    [Fact]
    public void The_same_input_gives_the_same_result()
    {
        var data = Data([_asha, _ravi], [.. Week(WeekB, _asha, 1), .. Week(WeekB, _ravi, 0)]);

        Assert.Equal(
            StarRules.For(data, WeekB, Sunday, Default)!.Children.Select(c => (c.ChildId, c.Total)),
            StarRules.For(data, WeekB, Sunday, Default)!.Children.Select(c => (c.ChildId, c.Total)));
    }

    private static CatchUpRow CatchUp(LedgerChildRow child, DateOnly week, string? outcome) =>
        new(child.Id, week, ["M3.ADD.CARRY"], Outcome: outcome);

    /// <summary>A full Mon–Sat week for one child with the first <paramref name="away"/> days away.</summary>
    private static RegisterRow[] Week(DateOnly monday, LedgerChildRow child, int away) =>
        Days(monday, child, [.. Enumerable.Range(0, 6).Select(i => i >= away)]);

    private static RegisterRow[] Days(DateOnly monday, LedgerChildRow child, bool[] present) =>
        [.. present.Select((p, i) => new RegisterRow(child.Id, monday.AddDays(i), p))];

    private static LedgerData Data(LedgerChildRow[] children, IEnumerable<RegisterRow> register, CatchUpRow[]? catchUps = null) =>
        new(children, [.. register], [], catchUps);

    private static LedgerData Data(LedgerChildRow[] children, params RegisterRow[][] registers) =>
        Data(children, registers.SelectMany(r => r));
}
