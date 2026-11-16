using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Tests.Features.LessonContents;

/// <summary>What a lesson's description and examples may be (T-141).</summary>
public sealed class LessonContentRulesTests
{
    [Fact]
    public void Trims_and_drops_blank_examples()
    {
        var (content, errors) = LessonContentRules.Normalise("  About it. ", ["  One. ", " ", null, "Two."]);

        Assert.Empty(errors);
        Assert.Equal("About it.", content!.Description);
        Assert.Equal(["One.", "Two."], content.Examples);
    }

    [Fact]
    public void All_blank_means_remove_it()
    {
        var (content, errors) = LessonContentRules.Normalise(" ", [" ", null]);

        Assert.Null(content);
        Assert.Empty(errors);
    }

    [Fact]
    public void Lengths_counts_and_braces_are_checked()
    {
        Assert.Contains("description", LessonContentRules.Normalise(new string('a', LessonContentRules.MaxDescription + 1), ["x"]).Errors.Keys);
        Assert.Contains("examples", LessonContentRules.Normalise("a", ["1", "2", "3", "4", "5"]).Errors.Keys);
        Assert.Contains("examples", LessonContentRules.Normalise("a", [new string('a', LessonContentRules.MaxExample + 1)]).Errors.Keys);
        Assert.Contains("examples", LessonContentRules.Normalise("a", ["{teacher} says"]).Errors.Keys);
        Assert.Contains("description", LessonContentRules.Normalise("", ["an example"]).Errors.Keys);
        Assert.Contains("examples", LessonContentRules.Normalise("a description", []).Errors.Keys);
        Assert.Null(LessonContentRules.Normalise(new string('a', LessonContentRules.MaxDescription), ["1", "2", "3", "4"]).Errors.GetValueOrDefault("description"));
    }
}
