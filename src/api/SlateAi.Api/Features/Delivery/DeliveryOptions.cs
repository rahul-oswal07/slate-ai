namespace SlateAi.Api.Features.Delivery;

public static class DeliveryChannels
{
    /// <summary>The simulated parent phone on /demo: it reads sent messages by token, so nothing leaves the server.</summary>
    public const string Demo = "demo";

    public const string WhatsApp = "whatsapp";
}

/// <summary>Which channel takes messages to families' phones (T-40, FR-16). <c>Delivery</c> section.</summary>
public sealed class DeliveryOptions
{
    public const string Section = "Delivery";

    /// <summary><see cref="DeliveryChannels.Demo"/> (default) or <see cref="DeliveryChannels.WhatsApp"/>.</summary>
    public string Channel { get; set; } = DeliveryChannels.Demo;

    /// <summary>Tries per message before it is marked failed; one try per minute (the 6 pm job's tick).</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Messages sent per tick at most.</summary>
    public int BatchSize { get; set; } = 50;
}

/// <summary>
/// WhatsApp Cloud API settings (<c>WhatsApp</c> section). <see cref="AccessToken"/> comes only from user-secrets or
/// App Service settings, never from the repo; so do the real numbers in <see cref="Recipients"/>.
/// </summary>
public sealed class WhatsAppOptions
{
    public const string Section = "WhatsApp";

    public string BaseUrl { get; set; } = "https://graph.facebook.com";
    public string ApiVersion { get; set; } = "v23.0";
    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }

    /// <summary>The approved template for each kind of message: name, and our language code to WhatsApp's.</summary>
    /// <remarks>
    /// Wording v4 (T-101, D-99) has no Meta template yet: <c>evening_absence_v2</c> is the name one would take, so an
    /// evening send can't reach a template with older text. T-104 decides what the plain evening message sends.
    /// </remarks>
    public WhatsAppTemplate Evening { get; set; } = new() { Name = "evening_absence_v2", WordingVersion = "4" };

    public WhatsAppTemplate CatchUp { get; set; } = new() { Name = "catchup_ready_v1" };

    /// <summary>
    /// Child ID → the family's number (E.164, e.g. +9198…) for the registered test phones, set as
    /// <c>WhatsApp__Recipients__{childId}</c>. Only these children get WhatsApp; everyone else stays on the demo chat.
    /// Seeded <c>ParentPhone</c> values are fictional and never dialled.
    /// </summary>
    public Dictionary<string, string> Recipients { get; set; } = [];

    /// <summary>The registered number for a child, or null. Keys are compared as GUIDs, so case doesn't matter.</summary>
    public string? RecipientFor(Guid childId) =>
        Recipients.FirstOrDefault(r => Guid.TryParse(r.Key, out var id) && id == childId).Value is { Length: > 0 } number
            ? number
            : null;
}

public sealed class WhatsAppTemplate
{
    public string Name { get; set; } = "";

    /// <summary>
    /// Evening only: the version of <c>data/seed/evening-message.json</c> whose text the Meta template copies word for
    /// word. A message the teacher approved at another version is skipped, so WhatsApp never carries wording she didn't
    /// approve (guardrail 3, D-49). A new wording version needs a new Meta template and this setting changed with it.
    /// </summary>
    public string? WordingVersion { get; set; }

    /// <summary>Our language code → the template's language code. Missing languages are not sent (skipped).</summary>
    public Dictionary<string, string> Languages { get; set; } = new()
    {
        ["en"] = "en",
        ["kn"] = "kn",
        ["te"] = "te",
        ["ta"] = "ta",
        ["hi"] = "hi",
    };
}
