using SlateAi.Api.Features.CatchUps;

namespace SlateAi.Api.Tests.Features.CatchUps;

/// <summary>
/// T-136: a catch-up cut off mid-writing (a restart or deploy empties the in-memory queue) stays "generating"; after
/// 5 minutes it reads as failed and Try again must take it.
/// </summary>
public sealed class StaleGeneratingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 14, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(PackageStatus.Generating, 6, true)]
    [InlineData(PackageStatus.Generating, 4, false)]
    [InlineData(PackageStatus.Failed, 0, false)]
    [InlineData(PackageStatus.Ready, 60, false)]
    public void A_catch_up_generating_for_over_5_minutes_is_stale(string status, int minutesAgo, bool stale)
    {
        Assert.Equal(stale, StaleGenerating.IsStale(status, Now.AddMinutes(-minutesAgo), Now));
    }

    [Theory]
    [InlineData(PackageStatus.Ready, 0, true)]
    [InlineData(PackageStatus.NeedsReview, 0, true)]
    [InlineData(PackageStatus.Failed, 0, true)]
    [InlineData(PackageStatus.Generating, 6, true)]
    [InlineData(PackageStatus.Generating, 1, false)]
    [InlineData(PackageStatus.Approved, 60, false)]
    [InlineData(PackageStatus.Sent, 60, false)]
    public void Try_again_takes_ready_needs_review_failed_and_stale(string status, int minutesAgo, bool allowed)
    {
        Assert.Equal(allowed, StaleGenerating.CanRegenerate(status, Now.AddMinutes(-minutesAgo), Now));
    }
}
