using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SlateAi.Api.Features.WhatsAppChat;

/// <summary>
/// One message a family sent us (T-106). <see cref="From"/> is their number as WhatsApp gives it (digits, no "+"): used to
/// check it against the saved numbers and to reply, never logged or stored.
/// </summary>
/// <param name="Id">WhatsApp's message ID (wamid): the key that makes a repeated event count once.</param>
/// <param name="ReplyId">The payload of a quick reply, or the ID of a reply button or list row we sent; null for text.</param>
/// <param name="Text">A typed message's text (a STOP reply), or null.</param>
public sealed record InboundMessage(string Id, string From, DateTimeOffset Timestamp, string? ReplyId, string? Text);

/// <summary>Meta's webhook POST: the signature check and the messages in it. Pure.</summary>
public static class WebhookPayload
{
    public const string SignatureHeader = "X-Hub-Signature-256";

    /// <summary>
    /// <c>X-Hub-Signature-256</c> is "sha256=" and the hex HMAC-SHA256 of the raw body with the app secret. Compared in
    /// constant time; a missing header, secret or malformed value is invalid.
    /// </summary>
    public static bool SignatureValid(ReadOnlySpan<byte> body, string? header, string? appSecret)
    {
        const string prefix = "sha256=";
        if (string.IsNullOrEmpty(appSecret) || header is null || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        byte[] given;
        try
        {
            given = Convert.FromHexString(header.AsSpan(prefix.Length));
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), body);
        return CryptographicOperations.FixedTimeEquals(expected, given);
    }

    /// <summary>The signature a body would have: for tests and local checks with a known secret.</summary>
    public static string Sign(ReadOnlySpan<byte> body, string appSecret) =>
        "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), body));

    /// <summary>
    /// The family messages in a <c>whatsapp_business_account</c> event: template quick replies ("button"), reply button
    /// and list taps ("interactive") and text. Delivery statuses and anything for another phone number ID are skipped.
    /// </summary>
    /// <exception cref="JsonException">The body isn't JSON.</exception>
    public static IReadOnlyList<InboundMessage> Messages(string json, string? phoneNumberId)
    {
        using var document = JsonDocument.Parse(json);
        List<InboundMessage> messages = [];
        foreach (var entry in Array(document.RootElement, "entry"))
        {
            foreach (var change in Array(entry, "changes"))
            {
                if (!change.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var to = value.TryGetProperty("metadata", out var metadata) ? String(metadata, "phone_number_id") : null;
                if (!string.IsNullOrEmpty(phoneNumberId) && to != phoneNumberId)
                {
                    continue;
                }

                foreach (var message in Array(value, "messages"))
                {
                    if (String(message, "id") is not { Length: > 0 } id
                        || String(message, "from") is not { Length: > 0 } from
                        || !long.TryParse(String(message, "timestamp"), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                    {
                        continue;
                    }

                    var (replyId, text) = String(message, "type") switch
                    {
                        "button" => (Object(message, "button") is { } button ? String(button, "payload") : null, null),
                        "interactive" => (Object(message, "interactive") is { } interactive
                            ? (Object(interactive, "button_reply") ?? Object(interactive, "list_reply")) is { } reply ? String(reply, "id") : null
                            : null, null),
                        "text" => ((string?)null, Object(message, "text") is { } body ? String(body, "body") : null),
                        _ => (null, null),
                    };
                    messages.Add(new InboundMessage(id, from, DateTimeOffset.FromUnixTimeSeconds(seconds), replyId, text));
                }
            }
        }

        return messages;
    }

    private static IEnumerable<JsonElement> Array(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().ToList()
            : [];

    private static JsonElement? Object(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    private static string? String(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
