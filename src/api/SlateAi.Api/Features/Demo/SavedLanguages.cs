using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Demo;

/// <summary>A home language changed on the admin page (T-122), kept across the demo Reset.</summary>
public sealed record SavedLanguage(string Language, DateTimeOffset SetAt);

public static class SavedLanguages
{
    /// <summary>
    /// Puts each child's changed home language back on the re-created rows (D-113: like saved numbers, D-110, so test
    /// families are set once). Children without one keep the seed's language.
    /// </summary>
    public static void Apply(IEnumerable<Child> children, IReadOnlyDictionary<Guid, SavedLanguage> saved)
    {
        foreach (var child in children)
        {
            if (saved.TryGetValue(child.Id, out var language))
            {
                child.HomeLanguage = language.Language;
                child.HomeLanguageSetAt = language.SetAt;
            }
        }
    }
}
