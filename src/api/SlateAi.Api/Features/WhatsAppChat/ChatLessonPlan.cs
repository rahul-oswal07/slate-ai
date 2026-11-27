using System.Globalization;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Features.WhatsAppChat;

/// <summary>
/// An evening message carrying today's message, as the chat needs it (T-106). Names and numbers are allowed here: this
/// is the final send to the family (guardrail 1); they are never logged.
/// </summary>
/// <param name="Phones">The family's saved numbers (E.164, D-114, D-122): only a tap from one of them is answered.</param>
/// <param name="TodaysApproved">Today's message is approved at its current version (guardrail 3); nothing is sent otherwise.</param>
/// <param name="Slots">The evening message's lesson names and page with its book, built by code when it was sent.</param>
/// <param name="PictureAnswer">The picture question's answer, for scoring only; never sent.</param>
/// <param name="Reason">The reason for absence already given (FR-21), or null: then the list is sent.</param>
/// <param name="DisclosureClipId">The AI disclosure in the family's language (D-18), or null: then no lesson audio.</param>
/// <param name="ConceptClipId">Today's concept, voiced (T-88), or null.</param>
public sealed record ChatLesson(
    Guid ChildId,
    Guid ClassId,
    DateOnly Date,
    string Language,
    string ChildName,
    string Consent,
    bool Stopped,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<string> Phones,
    bool TodaysApproved,
    SlotValues Slots,
    ParentTodaysMessage? Todays,
    string? PictureAnswer,
    string? Reason,
    Guid? GreetingClipId,
    Guid? DisclosureClipId,
    Guid? ConceptClipId);

/// <summary>A voice note still to upload: these clips, joined in order into one Ogg Opus file (D-127).</summary>
public sealed record ChatVoice(IReadOnlyList<Guid> ClipIds) : ChatMessage
{
    /// <summary>The media ID cache's key: the same clips in the same order are uploaded once.</summary>
    public string Key => string.Join('+', ClipIds);
}

/// <summary>
/// What goes into the chat after "Send it here", in order, and the replies to taps (T-106). Everything comes from the
/// approved today's message, the evening message's code-built slots and the fixed lines (<see cref="ChatWording"/>):
/// no LLM, and numbers by code (guardrail 4). Pure.
/// </summary>
public static class ChatLessonPlan
{
    /// <summary>WhatsApp's customer service window: free-form messages only this long after the family's last message.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    /// <summary>The window opened by a message received at <paramref name="received"/> is still open.</summary>
    public static bool WindowOpen(DateTimeOffset received, DateTimeOffset now) => now < received + Window;

    /// <summary>
    /// (1) her greeting as a voice note, (2) the lesson audio: the AI disclosure and the concept as one voice note (D-127),
    /// or two in a row when <paramref name="joinDisclosure"/> is false, (3) one text: lesson, concept, activity steps,
    /// homework page, video link and her note, (4) the picture question with its reply buttons, unless both tries are
    /// used, (5) the reason list, unless a reason was given. A part without content is left out; the text always goes, so
    /// it carries the lesson even if audio never loads.
    /// </summary>
    public static IReadOnlyList<ChatMessage> Lesson(ChatLesson lesson, string token, ChatWords words, bool joinDisclosure)
    {
        var todays = lesson.Todays ?? throw new ArgumentException("No today's message.", nameof(lesson));
        List<ChatMessage> messages = [];
        if (lesson.GreetingClipId is { } greeting)
        {
            messages.Add(new ChatVoice([greeting]));
        }

        // As on the family page: the lesson audio only behind the AI disclosure (D-18).
        if (lesson is { ConceptClipId: { } concept, DisclosureClipId: { } disclosure })
        {
            messages.AddRange(joinDisclosure
                ? [new ChatVoice([disclosure, concept])]
                : [new ChatVoice([disclosure]), new ChatVoice([concept])]);
        }

        messages.Add(new ChatText(Text(lesson, todays, words)));
        if (todays.Picture is { Locked: false } picture && Picture(picture, token, lesson.ChildName, words) is { } question)
        {
            messages.Add(question);
        }

        if (lesson.Reason is null)
        {
            messages.Add(new ChatList(
                ChatWording.Fill(words.ReasonQuestion, ("child", lesson.ChildName)),
                words.ReasonButton,
                ReasonCategories
                    .Select(c => new ChatOption(ChatReplyIds.ForReason(token, c), words.Reasons.GetValueOrDefault(c, c)))
                    .ToList()));
        }

        return messages;
    }

    /// <summary>The five reasons in the family page's order (FR-21, D-23).</summary>
    public static readonly string[] ReasonCategories =
    [
        ReasonCategory.Unwell, ReasonCategory.TravelOrWork, ReasonCategory.Festival, ReasonCategory.NeededAtHome,
        ReasonCategory.Other,
    ];

    /// <summary>What may be tapped: &gt;, &lt; or =, or for a sum its three numbers (D-116), as the family page allows.</summary>
    public static IReadOnlyList<string> Choices(ParentPicture picture) =>
        picture.Options is { Count: > 0 } options
            ? options.Select(n => n.ToString(CultureInfo.InvariantCulture)).ToList()
            : TodaysLevel.Signs;

    /// <summary>
    /// The reply to a tap (D-108: never right or wrong): after the first, their choice and that they may change it once;
    /// after the second, or when both were already used (<paramref name="after"/> null), thank you.
    /// </summary>
    public static string PictureReply(ChatWords words, string childName, string choice, (string? First, string? Second)? after) =>
        after is { Second: null }
            ? ChatWording.Fill(words.PictureAgain, ("child", childName), ("choice", choice))
            : ChatWording.Fill(words.PictureDone, ("child", childName));

    private static string Text(ChatLesson lesson, ParentTodaysMessage todays, ChatWords words)
    {
        List<string> parts = [todays.Concept is { Length: > 0 } concept ? $"*{lesson.Slots.Lesson}*\n{concept}" : $"*{lesson.Slots.Lesson}*"];
        if (todays.Steps is { Count: > 0 } steps)
        {
            parts.Add($"*{words.Activity}*\n" + string.Join('\n', steps.Select((s, i) => $"{i + 1}. {s}")));
        }

        parts.Add(ChatWording.Fill(words.Homework, ("page", lesson.Slots.Page)));
        if (todays.Video is { } video)
        {
            parts.Add($"*{words.Video}*\n{video.Url}\n{words.VideoData}");
        }

        if (todays.Note is { Length: > 0 } note)
        {
            parts.Add(note);
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// The picture question as text with reply buttons: "47 ? 52" with &gt; &lt; =, or "248 + 438 = ?" with its three
    /// numbers. No image yet (no drawing library in the app): the numbers are written, the family page draws the sticks.
    /// </summary>
    private static ChatButtons? Picture(ParentPicture picture, string token, string childName, ChatWords words)
    {
        var title = ChatWording.Fill(words.PictureTitle, ("child", childName));
        var choices = Choices(picture);
        string a = Number(picture.A), b = Number(picture.B);
        var (question, prompt) = picture.Kind switch
        {
            TodaysPicture.Compare => ($"{a}   ?   {b}", words.PictureCompare),
            TodaysPicture.Add when picture.Options is { Count: > 0 } => ($"{a} + {b} = ?", words.PictureSum),
            TodaysPicture.Sub when picture.Options is { Count: > 0 } => ($"{a} − {b} = ?", words.PictureSum),
            _ => (null, null),
        };
        return question is null
            ? null
            : new ChatButtons(
                $"*{title}*\n\n*{question}*\n\n{prompt}",
                choices.Select(c => new ChatOption(ChatReplyIds.ForPicture(token, c), c)).ToList());
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
