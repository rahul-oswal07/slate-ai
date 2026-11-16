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

    private readonly LedgerChildRow _asha = new(Guid.NewGuid(), 3, "Asha");
    private readonly LedgerChildRow _ravi = new(Guid.NewGuid(), 4, "Ravi");
    private readonly LedgerChildRow _kiran = new(Guid.NewGuid(), 9, "Kiran");

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(6, false)]
    public void A_finished_week_earns_a_star_only_with_every_school_day_present(int daysAway, bool earned)
    {
        var data = Data([_asha], Week(WeekA, _asha, daysAway));

        var stars = StarRules.For(data, WeekA, Sunday)!;

        Assert.True(stars.Finished);
        Assert.Equal(earned ? [_asha.Id] : [], stars.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void A_day_with_no_register_is_not_a_school_day_and_a_week_without_a_register_is_not_a_week()
    {
        // Three school days (Mon, Tue, Wed): Asha present all three earns it; Ravi away once doesn't. Nothing in week A.
        var data = Data([_asha, _ravi], Days(WeekB, _asha, [true, true, true]), Days(WeekB, _ravi, [true, false, true]));

        Assert.Null(StarRules.For(data, WeekA, Sunday));
        var week = StarRules.For(data, WeekB, Sunday)!;
        Assert.Equal(3, week.SchoolDays);
        Assert.Equal([_asha.Id], week.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void A_school_day_without_her_row_loses_it()
    {
        // The class has a register on Tuesday, but Kiran has no row that day (joined on Wednesday).
        var data = Data(
            [_asha, _kiran],
            Days(WeekB, _asha, [true, true, true]),
            [new RegisterRow(_kiran.Id, WeekB.AddDays(2), true)]);

        Assert.Equal([_asha.Id], StarRules.For(data, WeekB, Sunday)!.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void Away_every_school_day_earns_nothing_even_in_a_one_day_week()
    {
        var data = Data([_asha, _ravi], Days(WeekB, _asha, [false]), Days(WeekB, _ravi, [true]));

        Assert.Equal([_ravi.Id], StarRules.For(data, WeekB, Sunday)!.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void A_child_with_no_rows_that_week_is_skipped()
    {
        var data = Data([_asha, _kiran], Week(WeekB, _asha, 0));

        Assert.Equal([_asha.Id], StarRules.For(data, WeekB, Sunday)!.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void The_current_week_lists_the_children_present_every_day_so_far()
    {
        var wednesday = WeekC.AddDays(2);
        var data = Data(
            [_asha, _ravi],
            Days(WeekC, _asha, [true, true, true]),
            Days(WeekC, _ravi, [true, false, true]));

        var stars = StarRules.For(data, WeekC, wednesday)!;

        Assert.False(stars.Finished);
        Assert.Equal([_asha.Id], stars.Children.Select(c => c.ChildId));
    }

    [Fact]
    public void Saturday_is_finished_only_once_its_register_is_in()
    {
        var saturday = WeekC.AddDays(5);
        var without = Data([_asha], Days(WeekC, _asha, [true, true, true]));
        var with = Data([_asha], Days(WeekC, _asha, [true, true, true, true, true, true]));

        Assert.False(StarRules.For(without, WeekC, saturday)!.Finished);
        Assert.True(StarRules.For(with, WeekC, saturday)!.Finished);
    }

    [Fact]
    public void Each_week_stands_alone_whatever_came_before_or_was_answered_at_home()
    {
        // Asha: every day in A, away in B, every day in C; catch-ups answered in A and B change nothing.
        var data = Data(
            [_asha],
            [.. Week(WeekA, _asha, 0), .. Week(WeekB, _asha, 3), .. Week(WeekC, _asha, 0)],
            [CatchUp(_asha, WeekA, "caughtUp"), CatchUp(_asha, WeekB, "caughtUp"), CatchUp(_asha, WeekB, "needsBuddy")]);

        Assert.Single(StarRules.For(data, WeekA, Sunday)!.Children);
        Assert.Empty(StarRules.For(data, WeekB, Sunday)!.Children);
        Assert.Equal(new StarChild(_asha.Id, 3, "Asha"), StarRules.For(data, WeekC, Sunday)!.Children.Single());
    }

    [Fact]
    public void Children_come_in_roll_order_and_those_without_a_star_are_never_listed()
    {
        var data = Data(
            [_kiran, _ravi, _asha],
            [.. Week(WeekB, _kiran, 0), .. Week(WeekB, _ravi, 1), .. Week(WeekB, _asha, 0)]);

        Assert.Equal([3, 9], StarRules.For(data, WeekB, Sunday)!.Children.Select(c => c.RollNo));
    }

    [Fact]
    public void The_same_input_gives_the_same_result()
    {
        var data = Data([_asha, _ravi], [.. Week(WeekB, _asha, 1), .. Week(WeekB, _ravi, 0)]);

        Assert.Equal(StarRules.For(data, WeekB, Sunday)!.Children, StarRules.For(data, WeekB, Sunday)!.Children);
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
