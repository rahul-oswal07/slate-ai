using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Compression;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class CatchUpPlannerTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly Guid Manjunath = Guid.NewGuid();

    [Fact]
    public void A_child_with_no_target_gets_no_catch_up_and_no_failed_package()
    {
        // status.md 14 Nov (a), T-100: a lesson code can't make sums for never shows as "Generation failed".
        A2Result[] results =
        [
            new(Ravi, [new CompressionItem("M3.SUB.BORROW", null)], [], CompressionSource.Rule, "rule:no-candidates"),
            new(Manjunath, [], [new CompressionItem("M3.BRIDGE.COMPARE", null)], CompressionSource.Rule, "rule:no-candidates"),
        ];
        var languages = new Dictionary<Guid, string> { [Ravi] = "te", [Manjunath] = "kn" };

        var (packages, assignments) = CatchUpPlanner.Plan(ClassId, Monday, results, languages, CompressionBook.Graph);

        Assert.Equal([Ravi], assignments.Select(a => a.ChildId));
        var package = Assert.Single(packages);
        Assert.Equal("M3.SUB.BORROW", package.TargetKey);
        Assert.NotNull(package.Problems);
    }
}
