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

    /// <summary>
    /// The Meta app's secret (<b>secret</b>): the webhook checks <c>X-Hub-Signature-256</c> with it (T-106). Only from
    /// user-secrets or App Service settings. Without it every webhook POST is refused (401).
    /// </summary>
    public string? AppSecret { get; set; }

    /// <summary>The verify token typed into Meta's webhook setup; the webhook's GET echoes the challenge only for it (T-106).</summary>
    public string? VerifyToken { get; set; }

    /// <summary>Step 2: today's message inside the chat (T-106). Off by default.</summary>
    public WhatsAppChatLessonOptions ChatLesson { get; set; } = new();

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

    /// <summary>
    /// The weekly star (T-125, D-118): {{1}} child; button opens /p/{token}. Copies <c>whatsApp</c> in
    /// <c>data/seed/weekly-star.json</c> at <see cref="WhatsAppTemplate.WordingVersion"/>. No language until Meta
    /// approves it (English submitted 15 Nov): until then families keep the demo chat. Add one with
    /// <c>WhatsApp__WeeklyStar__Languages__en=en</c> once Meta approves it.
    /// </summary>
    public WhatsAppTemplate WeeklyStar { get; set; } = new()
    {
        Name = "weekly_star_v1",
        WordingVersion = "1",
        Languages = new(),
    };
}

public sealed class WhatsAppTemplate
{
    public string Name { get; set; } = "";

    /// <summary>
    /// Evening and weekly star: the version of the wording file (<c>data/seed/evening-message.json</c>,
    /// <c>data/seed/weekly-star.json</c>) whose text the Meta template copies word for
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

/// <summary>
/// Step 2 (T-106, D-98): today's message goes as <c>todays_lesson_chat_v1</c>, whose "Send it here" quick reply brings
/// the lesson into the chat through the webhook. <see cref="Enabled"/> is off by default: until Rahul switches it on
/// (after Meta enables the template and the webhook is set up) today's message goes as <c>todays_lesson_link_v1</c> and
/// the webhook answers 404.
/// </summary>
public sealed class WhatsAppChatLessonOptions
{
    /// <summary><c>WhatsApp:ChatLesson:Enabled</c>: false (default) keeps step 1 exactly as it is.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The template: {{1}} child, {{2}} lesson, {{3}} page (as <c>todays_lesson_link_v1</c>); button 0 is the quick reply
    /// "Send it here", button 1 the URL button that opens /p/{token}. A language missing here goes as the link template.
    /// </summary>
    public WhatsAppTemplate Template { get; set; } = new() { Name = "todays_lesson_chat_v1" };

    /// <summary>
    /// The AI disclosure and the lesson as one voice note (D-127, changing D-18's separate segments). False sends them as
    /// two voice notes in a row, the disclosure first.
    /// </summary>
    public bool JoinDisclosure { get; set; } = true;
}
