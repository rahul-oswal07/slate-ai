namespace SlateAi.Api.Features.Auth;

/// <summary>Indian mobile numbers, stored as the 10-digit national number.</summary>
public static class PhoneNumber
{
    /// <summary>
    /// Returns the 10-digit number, or null if the input isn't one. Spaces, dashes, a leading
    /// "+91"/"91" or a trunk "0" are accepted, so "+91 98450 12345" and "098450-12345" match.
    /// </summary>
    public static string? Normalise(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = new string(input.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        return digits.Length == 10 ? digits : null;
    }
}
