using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Demo;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>Home languages changed on the admin page survive the demo Reset (T-122, D-113).</summary>
public sealed class SavedLanguagesTests
{
    [Fact]
    public void Reset_puts_each_changed_language_back_and_leaves_the_others_on_the_seed()
    {
        var setAt = new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
        var manjunath = Child("Manjunath", "kn");
        var ravi = Child("Ravi", "te");

        SavedLanguages.Apply([manjunath, ravi], new Dictionary<Guid, SavedLanguage> { [manjunath.Id] = new("ta", setAt) });

        Assert.Equal(("ta", (DateTimeOffset?)setAt), (manjunath.HomeLanguage, manjunath.HomeLanguageSetAt));
        Assert.Equal(("te", (DateTimeOffset?)null), (ravi.HomeLanguage, ravi.HomeLanguageSetAt));
    }

    private static Child Child(string name, string language) =>
        new() { Id = Guid.NewGuid(), FirstName = name, HomeLanguage = language, HasPhone = true };
}
