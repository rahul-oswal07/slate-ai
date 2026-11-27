using System.Text.Json;

namespace SlateAi.Api.Features.WhatsAppChat;

/// <summary>
/// The fixed lines of one language in the chat (T-106): section titles, the picture question's prompt and replies, the
/// reason list and the stop reply. Code fills <c>{child}</c>, <c>{page}</c> and <c>{choice}</c>; nothing says right or
/// wrong (D-108).
/// </summary>
public sealed record ChatWords(
    string Activity,
    string Homework,
    string Video,
    string VideoData,
    string PictureTitle,
    string PictureCompare,
    string PictureSum,
    string PictureAgain,
    string PictureDone,
    string ReasonQuestion,
    string ReasonButton,
    IReadOnlyDictionary<string, string> Reasons,
    string ReasonThanks,
    string Stopped,
    IReadOnlyList<string> StopWords);

/// <summary>The chat's fixed lines per language (<c>data/seed/whatsapp-chat.json</c>).</summary>
public sealed record ChatWording(string Version, IReadOnlyDictionary<string, ChatWords> Languages)
{
    public const string File = "data/seed/whatsapp-chat.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static ChatWording Load(string path) =>
        JsonSerializer.Deserialize<ChatWording>(System.IO.File.ReadAllText(path), Json)
        ?? throw new InvalidOperationException($"Chat wording '{path}' is empty.");

    /// <summary>The lines in the family's language, or null when there are none (nothing is sent).</summary>
    public ChatWords? For(string language) => Languages.GetValueOrDefault(language);

    /// <summary>
    /// A reply that asks to stop (FR-19, D-19): "STOP" or the word in any of our languages, alone, any case, with
    /// punctuation around it ignored. A family may type English whatever their home language.
    /// </summary>
    public bool IsStop(string? text)
    {
        var word = text?.Trim().Trim('.', '!', '।', ' ').ToLowerInvariant();
        return !string.IsNullOrEmpty(word)
            && Languages.Values.Any(l => l.StopWords.Any(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Fills <c>{name}</c> slots; a slot without a value stays as it is.</summary>
    public static string Fill(string text, params (string Name, string Value)[] values) =>
        values.Aggregate(text, (current, v) => current.Replace($"{{{v.Name}}}", v.Value, StringComparison.Ordinal));
}
