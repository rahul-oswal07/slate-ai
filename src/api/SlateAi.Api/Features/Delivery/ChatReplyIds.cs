namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// What a tap in the chat sends back to the webhook (T-106): the quick reply's payload on <c>todays_lesson_chat_v1</c> and
/// the IDs of the reply buttons and list rows we send. Each carries the parent link token, so the webhook finds the
/// message without the phone number in any ID; the sender's number is still checked against the family's saved ones.
/// Tokens are URL-safe base64, so they never contain ":". Pure.
/// </summary>
public static class ChatReplyIds
{
    /// <summary>"Send it here" on the template.</summary>
    public const string Lesson = "lesson";

    /// <summary>A reply button of the picture question: &gt;, &lt;, = or one of a sum's numbers.</summary>
    public const string Picture = "pq";

    /// <summary>A row of the reason list (FR-21).</summary>
    public const string Reason = "rs";

    public static string ForLesson(string token) => $"{Lesson}:{token}";

    public static string ForPicture(string token, string choice) => $"{Picture}:{token}:{choice}";

    public static string ForReason(string token, string category) => $"{Reason}:{token}:{category}";

    /// <summary>The kind, token and value (empty for "Send it here") of an ID we made, or null for anything else.</summary>
    public static (string Kind, string Token, string Value)? Parse(string? id)
    {
        if (id?.Split(':', 3) is not [var kind, { Length: > 0 } token, .. var rest])
        {
            return null;
        }

        return (kind, rest) switch
        {
            (Lesson, []) => (kind, token, ""),
            (Picture or Reason, [{ Length: > 0 } value]) => (kind, token, value),
            _ => null,
        };
    }
}
