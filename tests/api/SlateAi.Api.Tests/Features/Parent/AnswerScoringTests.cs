using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Parent;

namespace SlateAi.Api.Tests.Features.Parent;

public sealed class AnswerScoringTests
{
    private static readonly CheckData Check = new("M3.SUB.BORROW", "sub", "52 − 27", [35, 25, 15], CorrectIndex: 1, Answer: 25, null);

    [Fact]
    public void The_stored_correct_position_is_caught_up()
    {
        Assert.Equal((true, CheckOutcome.CaughtUp), AnswerScoring.Score(Check, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Any_other_picture_needs_a_buddy(int choice)
    {
        Assert.Equal((false, CheckOutcome.NeedsBuddy), AnswerScoring.Score(Check, choice));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void A_choice_that_was_not_shown_is_not_scored(int choice)
    {
        Assert.Null(AnswerScoring.Score(Check, choice));
    }

    [Theory]
    [InlineData(PackageStatus.Approved, 2, 2, true)]
    [InlineData(PackageStatus.Sent, 2, 2, true)]
    [InlineData(PackageStatus.Sent, 1, 2, false)]
    [InlineData(PackageStatus.Approved, null, 2, false)]
    [InlineData(PackageStatus.Ready, 2, 2, false)]
    [InlineData(PackageStatus.NeedsReview, 2, 2, false)]
    [InlineData(PackageStatus.Generating, 2, 2, false)]
    [InlineData(PackageStatus.Failed, 2, 2, false)]
    public void Only_an_approved_version_is_served(string status, int? approved, int version, bool served)
    {
        Assert.Equal(served, ParentRules.CanServe(status, approved, version));
    }
}
