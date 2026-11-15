namespace SlateAi.Api.Features.LessonLinks;

/// <summary>What a lesson's video link may be (D-106): any absolute https URL, at most <see cref="MaxLength"/>. Pure.</summary>
public static class LessonLinkRules
{
    public const int MaxLength = 500;

    /// <summary>The link to store, null to remove it (blank), or an error for the admin page.</summary>
    public static (string? Url, string? Error) Normalise(string? input)
    {
        var url = input?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            return (null, null);
        }

        if (url.Length > MaxLength)
        {
            return (null, $"At most {MaxLength} characters.");
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && uri.Host.Length > 0
            ? (uri.AbsoluteUri, null)
            : (null, "Use a full https link, e.g. https://diksha.gov.in/…");
    }
}
