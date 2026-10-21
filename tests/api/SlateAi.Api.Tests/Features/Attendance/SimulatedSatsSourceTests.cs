using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;

namespace SlateAi.Api.Tests.Features.Attendance;

public sealed class SimulatedSatsSourceTests
{
    private static readonly Guid ClassId = Guid.Parse("0a1e03b3-23fa-5a7d-87e3-490d6ddae335");
    private static readonly IReadOnlyList<RosterChild> Roster =
        Enumerable.Range(1, 36).Select(i => new RosterChild(Guid.NewGuid(), i, $"Child {i}")).ToList();
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    // Wed 21 Oct 2026.
    private static readonly DateOnly Wednesday = new(2026, 10, 21);

    private static SimulatedSatsSource Create(Action<SatsOptions>? configure = null)
    {
        var options = new SatsOptions();
        configure?.Invoke(options);
        return new SimulatedSatsSource(Options.Create(options));
    }

    [Fact]
    public async Task The_same_class_and_date_always_give_the_same_register()
    {
        var first = Assert.IsType<SourceResult.Arrived>(await Create().FetchAsync(ClassId, Wednesday, Roster, Ct));
        var second = Assert.IsType<SourceResult.Arrived>(await Create().FetchAsync(ClassId, Wednesday, Roster, Ct));

        Assert.True(first.AbsentChildIds.SetEquals(second.AbsentChildIds));
    }

    [Fact]
    public async Task Arrives_at_9_05_ist_on_the_day()
    {
        var register = Assert.IsType<SourceResult.Arrived>(await Create().FetchAsync(ClassId, Wednesday, Roster, Ct));

        Assert.Equal(new DateTimeOffset(2026, 10, 21, 9, 5, 0, SchoolClock.Ist), register.ReceivedAt);
    }

    [Fact]
    public void Roughly_the_absence_rate_over_a_term()
    {
        var source = Create(o => o.ScenarioFile = "no-such-file.json");
        var days = Enumerable.Range(0, 120).Select(i => Wednesday.AddDays(i)).ToList();

        var share = days.Average(day => source.Simulate(ClassId, day, Roster).Count) / Roster.Count;

        Assert.InRange(share, 0.05, 0.11);
    }

    [Fact]
    public void Different_days_give_different_absentees()
    {
        var source = Create(o => o.ScenarioFile = "no-such-file.json");

        var registers = Enumerable.Range(0, 5).Select(i => source.Simulate(ClassId, Wednesday.AddDays(i), Roster)).ToList();

        Assert.Contains(registers.Skip(1), r => !r.SetEquals(registers[0]));
    }

    [Theory]
    [InlineData("2026-10-25")] // Sunday
    [InlineData("2026-10-10")] // second Saturday
    [InlineData("2026-10-20")] // configured holiday
    public async Task No_school_on_sundays_second_saturdays_and_holidays(string date)
    {
        var source = Create(o => o.Holidays = [new DateOnly(2026, 10, 20)]);

        Assert.IsType<SourceResult.NoSchool>(await source.FetchAsync(ClassId, DateOnly.Parse(date), Roster, Ct));
    }

    [Fact]
    public async Task Other_saturdays_are_school_days()
    {
        Assert.IsType<SourceResult.Arrived>(await Create().FetchAsync(ClassId, new DateOnly(2026, 10, 17), Roster, Ct));
    }

    [Fact]
    public void The_demo_story_marks_roll_4_absent_tuesday_to_thursday()
    {
        var source = Create();
        var ravi = Roster.Single(c => c.RollNo == 4).Id;

        Assert.Contains(ravi, source.Simulate(ClassId, new DateOnly(2026, 10, 20), Roster)); // Tue
        Assert.Contains(ravi, source.Simulate(ClassId, Wednesday, Roster));
        Assert.Contains(ravi, source.Simulate(ClassId, new DateOnly(2026, 10, 22), Roster)); // Thu
    }
}
