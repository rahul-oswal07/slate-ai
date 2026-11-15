using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Features.Lessons;

/// <summary>D-104: when a new note may replace the confirmed lesson.</summary>
public sealed class LessonWindowTests
{
    private static TodaysMessageRow Row(string status, int version = 2, int? approvedVersion = null, bool sent = false) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 16), ["M3.X"], status, version, approvedVersion, null, null, null,
            DateTimeOffset.UnixEpoch, sent);

    [Fact]
    public void No_todays_message_open_until_tonights_message_has_gone()
    {
        Assert.True(LessonWindow.CanReplaceConfirmed(null, eveningSent: false));
        Assert.False(LessonWindow.CanReplaceConfirmed(null, eveningSent: true));
    }

    [Theory]
    [InlineData(TodaysMessageStatus.Generating)]
    [InlineData(TodaysMessageStatus.Ready)]
    [InlineData(TodaysMessageStatus.Failed)]
    public void Todays_message_not_approved_yet_is_open(string status) =>
        Assert.True(LessonWindow.CanReplaceConfirmed(Row(status), eveningSent: false));

    [Fact]
    public void Approved_at_its_version_is_closed() =>
        Assert.False(LessonWindow.CanReplaceConfirmed(Row(TodaysMessageStatus.Approved, approvedVersion: 2), eveningSent: false));

    [Fact]
    public void Sent_is_closed() =>
        Assert.False(LessonWindow.CanReplaceConfirmed(Row(TodaysMessageStatus.Approved, approvedVersion: 2, sent: true), eveningSent: false));

    [Fact]
    public void Tonights_plain_message_gone_closes_it_even_with_an_unapproved_draft() =>
        Assert.False(LessonWindow.CanReplaceConfirmed(Row(TodaysMessageStatus.Ready), eveningSent: true));
}
