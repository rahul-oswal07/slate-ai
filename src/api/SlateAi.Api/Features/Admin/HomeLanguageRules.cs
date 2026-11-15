namespace SlateAi.Api.Features.Admin;

/// <summary>
/// The home languages a child can have (D-73), as chosen on the admin children page (T-122, D-113). Pure, so it is
/// unit-tested on its own.
/// </summary>
public static class HomeLanguageRules
{
    /// <summary>Kannada, English, Telugu, Tamil and Hindi: the family languages with messages, audio and catch-ups.</summary>
    public static readonly IReadOnlyList<string> All = ["kn", "en", "te", "ta", "hi"];

    public const string Invalid = "Use one of kn, en, te, ta or hi.";

    /// <summary>The language code to store (trimmed, lower case), or an error for anything else, blank included.</summary>
    public static (string? Language, string? Error) Normalise(string? input)
    {
        var code = input?.Trim().ToLowerInvariant();
        return code is not null && All.Contains(code) ? (code, null) : (null, Invalid);
    }
}
