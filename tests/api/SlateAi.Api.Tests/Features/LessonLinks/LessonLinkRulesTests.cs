using SlateAi.Api.Features.LessonLinks;

namespace SlateAi.Api.Tests.Features.LessonLinks;

/// <summary>D-106: any absolute https link; blank removes it.</summary>
public sealed class LessonLinkRulesTests
{
    [Theory]
    [InlineData("https://diksha.gov.in/play/content/do_1", "https://diksha.gov.in/play/content/do_1")]
    [InlineData("  https://www.youtube.com/watch?v=abc  ", "https://www.youtube.com/watch?v=abc")]
    public void Any_https_link_is_kept(string input, string url) =>
        Assert.Equal((url, (string?)null), LessonLinkRules.Normalise(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_removes_the_link(string? input) =>
        Assert.Equal(((string?)null, (string?)null), LessonLinkRules.Normalise(input));

    [Theory]
    [InlineData("http://diksha.gov.in/play/content/do_1")]
    [InlineData("diksha.gov.in/play")]
    [InlineData("/play/content/do_1")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.org/file")]
    [InlineData("not a link")]
    public void Anything_else_is_refused(string input)
    {
        var (url, error) = LessonLinkRules.Normalise(input);

        Assert.Null(url);
        Assert.NotNull(error);
    }

    [Fact]
    public void A_link_longer_than_500_characters_is_refused() =>
        Assert.NotNull(LessonLinkRules.Normalise("https://diksha.gov.in/" + new string('a', 500)).Error);
}
