using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Features.TodaysMessage;

/// <summary>When the 6 pm job holds a class's evening messages for today's message (T-111, D-100).</summary>
public sealed class TodaysMessageWaitTests
{
    private static readonly DateOnly Today = new(2026, 11, 14);
    private static readonly DateTimeOffset SendAt = Ist(18, 0);
    private static readonly TimeSpan Wait = TimeSpan.FromMinutes(5);

    [Fact]
    public void No_todays_message_never_holds() =>
        Assert.False(TodaysMessageWait.Holds(null, Ist(18, 0), SendAt, Wait));

    [Fact]
    public void A_draft_being_written_holds_until_it_is_stale()
    {
        var row = Row(TodaysMessageStatus.Generating, Ist(18, 0));

        Assert.True(TodaysMessageWait.Holds(row, Ist(18, 0).AddSeconds(16), SendAt, Wait));
        Assert.True(TodaysMessageWait.Holds(row, Ist(18, 0) + CatchUpEndpoints.StaleGenerating, SendAt, Wait));
        Assert.False(TodaysMessageWait.Holds(row, Ist(18, 0) + CatchUpEndpoints.StaleGenerating + TimeSpan.FromSeconds(1), SendAt, Wait));
    }

    [Fact]
    public void A_ready_draft_holds_for_the_wait_from_when_it_was_ready()
    {
        var row = Row(TodaysMessageStatus.Ready, Ist(19, 10));

        Assert.True(TodaysMessageWait.Holds(row, Ist(19, 14), SendAt, Wait));
        Assert.False(TodaysMessageWait.Holds(row, Ist(19, 15), SendAt, Wait));
    }

    [Fact]
    public void A_draft_ready_before_the_send_time_holds_for_the_wait_from_the_send_time()
    {
        var row = Row(TodaysMessageStatus.Ready, Ist(13, 0));

        Assert.True(TodaysMessageWait.Holds(row, Ist(18, 4), SendAt, Wait));
        Assert.False(TodaysMessageWait.Holds(row, Ist(18, 5), SendAt, Wait));
    }

    [Fact]
    public void An_edit_starts_the_wait_again()
    {
        // Every edit is a new version with a new UpdatedAt (D-92).
        var edited = Row(TodaysMessageStatus.Ready, Ist(18, 4));

        Assert.True(TodaysMessageWait.Holds(edited, Ist(18, 8), SendAt, Wait));
        Assert.False(TodaysMessageWait.Holds(edited, Ist(18, 9), SendAt, Wait));
    }

    [Theory]
    [InlineData(TodaysMessageStatus.Approved)]
    [InlineData(TodaysMessageStatus.Failed)]
    public void An_approved_or_failed_message_never_holds(string status) =>
        Assert.False(TodaysMessageWait.Holds(Row(status, Ist(18, 0)), Ist(18, 1), SendAt, Wait));

    [Fact]
    public void A_message_families_already_have_never_holds() =>
        Assert.False(TodaysMessageWait.Holds(Row(TodaysMessageStatus.Ready, Ist(18, 0)) with { Sent = true }, Ist(18, 1), SendAt, Wait));

    private static DateTimeOffset Ist(int hour, int minute) => SchoolClock.At(Today, new TimeOnly(hour, minute));

    private static TodaysMessageRow Row(string status, DateTimeOffset updatedAt) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Today, ["M3.BRIDGE.COMPARE"], status, 1, null, null, null, null, updatedAt, false);
}
