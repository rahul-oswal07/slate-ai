using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Demo;

namespace SlateAi.Api.Tests.Features.Demo;

public sealed class DemoStoryTests
{
    private static readonly IReadOnlyList<RosterChild> Roster =
        Enumerable.Range(1, 36).Select(roll => new RosterChild(Guid.NewGuid(), roll, $"Child {roll}")).ToList();

    private static readonly Guid Ravi = Roster.Single(c => c.RollNo == DemoStory.RaviRollNo).Id;
    private static readonly Guid Manjunath = Roster.Single(c => c.RollNo == DemoStory.ManjunathRollNo).Id;
    private static readonly Guid Rohan = Roster.Single(c => c.RollNo == DemoStory.RohanRollNo).Id;

    private static readonly DateOnly Friday = new(2026, 10, 23);
    private static readonly DateOnly Monday = new(2026, 10, 19);

    [Fact]
    public void On_a_friday_ravi_has_been_away_monday_to_thursday_and_present_last_week()
    {
        var days = Build(Friday);

        // Sat 17 Oct is the third Saturday, a school day.
        Assert.Equal(
            [new(2026, 10, 17), new(2026, 10, 19), new(2026, 10, 20), new(2026, 10, 21), new(2026, 10, 22)],
            days.Select(d => d.Date).ToList());
        Assert.Equal(
            [false, true, true, true, true],
            days.Select(d => d.AbsentChildIds.Contains(Ravi)).ToList());
    }

    [Fact]
    public void Five_school_days_before_today_skip_sundays_and_the_second_saturday_and_leave_today_alone()
    {
        // Sat 10 Oct is the second Saturday; Sun 11 Oct is a Sunday.
        var days = Build(new DateOnly(2026, 10, 14));

        Assert.Equal(
            [new(2026, 10, 7), new(2026, 10, 8), new(2026, 10, 9), new(2026, 10, 12), new(2026, 10, 13)],
            days.Select(d => d.Date).ToList());
    }

    [Fact]
    public void On_a_monday_every_seeded_day_is_last_week_so_ravi_was_present()
    {
        var days = Build(Monday);

        Assert.All(days, d => Assert.True(d.Date < Monday));
        Assert.All(days, d => Assert.DoesNotContain(Ravi, d.AbsentChildIds));
    }

    [Fact]
    public void Manjunath_is_away_the_two_days_before_today_and_rohan_only_today()
    {
        var days = Build(Friday);

        Assert.Equal([false, false, false, true, true], days.Select(d => d.AbsentChildIds.Contains(Manjunath)).ToList());
        Assert.All(days, d => Assert.DoesNotContain(Rohan, d.AbsentChildIds));
    }

    [Fact]
    public void Nobody_else_is_away_on_the_seeded_days()
    {
        var days = Build(Friday);

        Assert.Equal(new[] { Ravi, Manjunath }.Order(), days.SelectMany(d => d.AbsentChildIds).Distinct().Order());
    }

    [Fact]
    public void Lessons_follow_the_book_up_to_the_one_before_carrying()
    {
        var days = Build(Friday);

        Assert.Equal(DemoStory.Lessons, days.Select(d => d.CompetencyId).ToList());
        Assert.Equal("M3.ADD.NOCARRY", days[^1].CompetencyId);
    }

    private static IReadOnlyList<DemoDay> Build(DateOnly today) =>
        DemoStory.Build(
            today,
            Roster,
            date => date.DayOfWeek != DayOfWeek.Sunday && !(date.DayOfWeek == DayOfWeek.Saturday && (date.Day - 1) / 7 == 1));
}
