using SlateAi.Api.Features.Admin;

namespace SlateAi.Api.Tests.Features.Admin;

/// <summary>The home languages the admin children page accepts (T-122, D-113).</summary>
public sealed class HomeLanguageRulesTests
{
    [Theory]
    [InlineData("kn")]
    [InlineData("en")]
    [InlineData("te")]
    [InlineData("ta")]
    [InlineData("hi")]
    public void Every_family_language_is_accepted(string code) =>
        Assert.Equal((code, (string?)null), HomeLanguageRules.Normalise(code));

    [Fact]
    public void Spaces_and_capitals_are_ignored() =>
        Assert.Equal(("kn", (string?)null), HomeLanguageRules.Normalise(" KN "));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("fr")]
    [InlineData("Kannada")]
    [InlineData("kn-IN")]
    public void Anything_else_is_refused(string? input) =>
        Assert.Equal(((string?)null, HomeLanguageRules.Invalid), HomeLanguageRules.Normalise(input));
}
