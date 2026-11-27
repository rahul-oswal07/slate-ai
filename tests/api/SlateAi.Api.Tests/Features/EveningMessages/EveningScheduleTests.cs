using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.LessonAudio;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Features.EveningMessages;

/// <summary>When tonight's message goes if she does nothing (T-86): the job's hold, as Today's card shows it.</summary>
public sealed class EveningScheduleTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private static readonly DateTimeOffset SendAt = Ist(18, 0);
    private static readonly EveningOptions Options = new();

    [Fact]
    public void Without_todays_message_it_goes_at_the_send_time()
    {
        var next = Next(null, Ist(15, 0));

        Assert.Equal(SendAt, next.GoesAt);
        Assert.Null(next.Waiting);
        Assert.False(next.Holds(Ist(18, 0)));
    }

    [Fact]
    public void Approved_failed_or_sent_todays_message_does_not_wait()
    {
        Assert.Equal(new EveningNext(SendAt, null), Next(Row(TodaysMessageStatus.Approved, Ist(13, 0)), Ist(15, 0)));
        Assert.Equal(new EveningNext(SendAt, null), Next(Row(TodaysMessageStatus.Failed, Ist(13, 0)), Ist(18, 2)));
        Assert.Equal(new EveningNext(SendAt, null), Next(Row(TodaysMessageStatus.Ready, Ist(18, 1)) with { Sent = true }, Ist(18, 2)));
    }

    [Fact]
    public void Ready_before_6_pm_waits_for_her_until_6_05()
    {
        var row = Row(TodaysMessageStatus.Ready, Ist(13, 0));

        var next = Next(row, Ist(15, 0));

        Assert.Equal(new EveningNext(Ist(18, 5), EveningWaiting.ForYou), next);
        Assert.True(next.Holds(Ist(18, 4)));
        Assert.False(next.Holds(Ist(18, 5)));
    }

    [Fact]
    public void Ready_after_6_pm_waits_5_minutes_from_when_it_was_ready()
    {
        var row = Row(TodaysMessageStatus.Ready, Ist(19, 10));

        Assert.Equal(new EveningNext(Ist(19, 15), EveningWaiting.ForYou), Next(row, Ist(19, 12)));
        // At 19:15 the wait is over: the plain message goes now.
        Assert.Equal(new EveningNext(SendAt, null), Next(row, Ist(19, 15)));
    }

    [Fact]
    public void Being_written_has_no_known_end_until_it_is_stuck()
    {
        var row = Row(TodaysMessageStatus.Generating, Ist(18, 0));

        var next = Next(row, Ist(18, 1));
        Assert.Equal(new EveningNext(null, EveningWaiting.Writing), next);
        Assert.True(next.Holds(Ist(18, 1)));

        // Generating far longer than it takes: the app restarted mid-way, so nothing holds it.
        Assert.Equal(new EveningNext(SendAt, null), Next(row, Ist(18, 0) + StaleGenerating.After + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void A_ticked_lesson_clip_holds_the_same_way()
    {
        var clip = new LessonAudioRow(
            Guid.NewGuid(), Guid.NewGuid(), Today, "kn", Guid.NewGuid(), LessonAudioStatus.Ready, null, null, Ist(18, 3));

        var next = EveningSchedule.Next([clip], Row(TodaysMessageStatus.Ready, Ist(18, 1)), Ist(18, 4), SendAt, Options);

        // The later of the two waits.
        Assert.Equal(new EveningNext(Ist(18, 8), EveningWaiting.ForYou), next);
    }

    private static EveningNext Next(TodaysMessageRow? row, DateTimeOffset now) =>
        EveningSchedule.Next([], row, now, SendAt, Options);

    private static DateTimeOffset Ist(int hour, int minute) => SchoolClock.At(Today, new TimeOnly(hour, minute));

    private static TodaysMessageRow Row(string status, DateTimeOffset updatedAt) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Today, ["M3.BRIDGE.COMPARE"], status, 1, null, null, null, null, updatedAt, false);
}
