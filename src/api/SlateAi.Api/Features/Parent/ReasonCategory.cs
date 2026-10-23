namespace SlateAi.Api.Features.Parent;

/// <summary>The five picture buttons on the reason page (FR-21, D-23). A category, never a diagnosis.</summary>
public static class ReasonCategory
{
    public const string Unwell = "unwell";
    public const string TravelOrWork = "travelOrWork";
    public const string Festival = "festival";
    public const string NeededAtHome = "neededAtHome";
    public const string Other = "other";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Unwell, TravelOrWork, Festival, NeededAtHome, Other };
}
