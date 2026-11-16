using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Demo;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>The fictional demo class in <c>data/seed/demo-class.json</c>.</summary>
public sealed class DemoSeedFileTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_demo_class_has_families_in_every_language_two_of_them_Hindi_and_two_English()
    {
        // T-131: two Hindi (Rohan, Pooja) and two English (Sanjay, Shreya) families, so the demo shows both on the
        // family phones and in Under the hood. Manjunath, the story child, stays Kannada (T-93).
        var seed = await DemoSeedFile.LoadAsync("data/seed/demo-class.json", Ct);

        var byLanguage = seed.Children.GroupBy(c => c.HomeLanguage).ToDictionary(g => g.Key, g => g.Select(c => c.FirstName).Order().ToList());

        Assert.Equal(36, seed.Children.Count);
        Assert.Equal(["Pooja", "Rohan"], byLanguage["hi"]);
        Assert.Equal(["Sanjay", "Shreya"], byLanguage["en"]);
        Assert.Equal((23, 6, 3), (byLanguage["kn"].Count, byLanguage["te"].Count, byLanguage["ta"].Count));
        Assert.Equal("kn", seed.Children.Single(c => c.RollNo == DemoStory.ManjunathRollNo).HomeLanguage);
    }
}
