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
/// App Service settings, never from the repo. Families' numbers are saved on the admin page, never in settings (D-114).
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
    /// The plain evening message (D-21) has no approved template (D-107): Meta classed the warm wording as Marketing,
    /// and its factual replacement isn't submitted yet. No name means it is skipped (<c>no-template</c>) and the family
    /// keeps the demo chat. Once approved, set <c>WhatsApp:Evening:Name</c> and the wording version it copies.
    /// </remarks>
    public WhatsAppTemplate Evening { get; set; } = new() { Name = "", WordingVersion = "4" };

    /// <summary>Today's message (D-98): {{1}} child, {{2}} lesson, {{3}} page number; button opens /p/{token}.</summary>
    public WhatsAppTemplate Todays { get; set; } = new() { Name = "todays_lesson_link_v1" };

    /// <summary>The catch-up (D-98): {{1}} child, {{2}} lesson names, {{3}} pages with the word; button opens /p/{token}.</summary>
    /// <remarks>Approved in en, kn, te and hi (15 Nov); no Tamil yet, so a Tamil catch-up stays on the demo chat.</remarks>
    public WhatsAppTemplate CatchUp { get; set; } = new()
    {
        Name = "catchup_link_v1",
        Languages = new() { ["en"] = "en", ["kn"] = "kn", ["te"] = "te", ["hi"] = "hi" },
    };
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
