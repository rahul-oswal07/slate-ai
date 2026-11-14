namespace SlateAi.Api.Features.Auth;

/// <summary>
/// What of a phone number may leave the server for her own Profile screen (T-109): the last 4 digits, never the
/// whole number.
/// </summary>
public static class PhoneMask
{
    /// <summary>The last 4 digits, or null when there are fewer than 4 (nothing worth showing).</summary>
    public static string? Last4(string? phone)
    {
        if (phone is null)
        {
            return null;
        }

        var digits = string.Concat(phone.Where(char.IsAsciiDigit));
        return digits.Length < 4 ? null : digits[^4..];
    }
}
