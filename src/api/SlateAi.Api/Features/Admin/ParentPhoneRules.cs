using System.Text.RegularExpressions;

namespace SlateAi.Api.Features.Admin;

/// <summary>
/// What a number typed on the admin children page may be (T-118). Stored as E.164 (<c>+919876543210</c>), the form
/// WhatsApp takes. Pure, so it is unit-tested on its own.
/// </summary>
public static partial class ParentPhoneRules
{
    public const string Invalid =
        "Use a 10-digit Indian mobile number (e.g. 98765 43210) or a full number with its country code (e.g. +44 7700 900123).";

    /// <summary>
    /// The number to store, or null to clear it (blank input). Spaces, dashes, dots and brackets are ignored; a 10-digit
    /// Indian mobile (6–9 first) gets <c>+91</c>; <c>91</c> + 10 digits and <c>0</c> + 10 digits are read the same way.
    /// </summary>
    public static (string? Phone, string? Error) Normalise(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (null, null);
        }

        var compact = Separators().Replace(input.Trim(), "");
        if (IndianMobile().Match(compact) is { Success: true } indian)
        {
            return ($"+91{indian.Groups["number"].Value}", null);
        }

        return E164().IsMatch(compact) && !compact.StartsWith("+91", StringComparison.Ordinal) ? (compact, null) : (null, Invalid);
    }

    [GeneratedRegex(@"[\s\-.()]")]
    private static partial Regex Separators();

    /// <summary>98765 43210, 098765 43210, 919876543210 or +919876543210.</summary>
    [GeneratedRegex(@"^(?:\+91|91|0)?(?<number>[6-9]\d{9})$")]
    private static partial Regex IndianMobile();

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164();
}
