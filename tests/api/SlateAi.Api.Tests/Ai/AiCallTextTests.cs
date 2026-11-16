using SlateAi.Api.Ai;

namespace SlateAi.Api.Tests.Ai;

/// <summary>What goes into the AI log must fit its columns, so logging can never fail the work it logs (T-137).</summary>
public sealed class AiCallTextTests
{
    [Fact]
    public void A_long_verdict_is_cut_to_the_column_and_a_short_one_is_kept()
    {
        // A4's "fail:" plus several reasons ran past 100 characters and failed a whole catch-up (T-137).
        var reasons = "fail:" + string.Join("; ", Enumerable.Repeat("the summary doesn't match the script", 5));

        Assert.True(reasons.Length > 100);
        Assert.Equal(100, AiCallText.Verdict(reasons).Length);
        Assert.StartsWith("fail:the summary", AiCallText.Verdict(reasons));
        Assert.Equal("pass", AiCallText.Verdict("pass"));
    }
}
