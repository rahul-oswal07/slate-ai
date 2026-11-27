using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.WhatsAppChat;

/// <summary>What the webhook did with one message; logged with the hashed message ID, and returned for tests.</summary>
public static class ChatOutcome
{
    public const string Duplicate = "duplicate";
    public const string LessonSent = "lesson-sent";
    public const string PictureSaved = "picture";
    public const string ReasonSaved = "reason";
    public const string StopSaved = "stop";
    public const string UnknownToken = "unknown-token";
    public const string UnknownSender = "unknown-sender";
    public const string NotApproved = "not-approved";
    public const string WindowClosed = "window-closed";
    public const string BadChoice = "bad-choice";
    public const string Ignored = "ignored";
}

/// <summary>
/// WhatsApp media IDs of uploaded voice notes by their clips (T-106): each clip (or joined pair) is uploaded once per
/// lesson and language and its ID reused for every family. Kept in memory, 29 days (Meta keeps media 30): after a restart
/// a clip is uploaded once more.
/// </summary>
public sealed class WhatsAppMediaCache
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(29);

    private readonly ConcurrentDictionary<string, (string MediaId, DateTimeOffset UploadedAt)> _ids = new();

    public string? Get(string key, DateTimeOffset now) =>
        _ids.TryGetValue(key, out var entry) && now < entry.UploadedAt + Lifetime ? entry.MediaId : null;

    public void Set(string key, string mediaId, DateTimeOffset now) => _ids[key] = (mediaId, now);
}

/// <summary>
/// Handles one family message from the webhook (T-106). Each event is handled once (by its wamid). "Send it here" sends
/// today's message into the chat, only when it is approved at its current version (guardrail 3), consent is yes, the
/// family hasn't stopped and the link hasn't expired; a picture tap or reason is saved as on the family page; STOP stops
/// the family's messages. Only a tap from one of the family's saved numbers counts. Nothing is sent once the 24-hour
/// window has closed. Logs IDs and codes only: never a name, number or text.
/// </summary>
public sealed class ChatHandler(
    IWhatsAppChatStore store,
    WhatsAppClient client,
    WhatsAppMediaCache media,
    ChatWording wording,
    IOptions<WhatsAppOptions> options,
    TimeProvider clock,
    ILogger<ChatHandler> logger)
{
    public async Task<string> HandleAsync(InboundMessage inbound, CancellationToken cancellationToken)
    {
        var eventId = ProviderMessageIds.Hash(inbound.Id);
        string outcome;
        if (!await store.TryRecordEventAsync(EventGuid("event", inbound.Id), clock.GetUtcNow(), cancellationToken))
        {
            outcome = ChatOutcome.Duplicate;
        }
        else if (ChatReplyIds.Parse(inbound.ReplyId) is { } reply)
        {
            outcome = reply.Kind switch
            {
                ChatReplyIds.Lesson => await LessonAsync(inbound, reply.Token, eventId, cancellationToken),
                ChatReplyIds.Picture => await PictureAsync(inbound, reply.Token, reply.Value, eventId, cancellationToken),
                _ => await ReasonAsync(inbound, reply.Token, reply.Value, eventId, cancellationToken),
            };
        }
        else
        {
            outcome = wording.IsStop(inbound.Text) ? await StopAsync(inbound, eventId, cancellationToken) : ChatOutcome.Ignored;
        }

        logger.LogInformation("WhatsApp event {EventId}: {Outcome}", eventId, outcome);
        return outcome;
    }

    /// <summary>A stable ID for one use of one wamid (the event itself, its picture submit), for <c>ProcessedRequests</c>.</summary>
    public static Guid EventGuid(string purpose, string messageId) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"whatsapp:{purpose}:{messageId}")).AsSpan(0, 16));

    private async Task<string> LessonAsync(InboundMessage inbound, string token, string eventId, CancellationToken cancellationToken)
    {
        var (lesson, problem) = await FindAsync(inbound, token, cancellationToken);
        if (lesson is null)
        {
            return problem;
        }

        var now = clock.GetUtcNow();
        if (!lesson.TodaysApproved || lesson.Todays is null)
        {
            return ChatOutcome.NotApproved;
        }

        if (DeliveryRules.Block(lesson.Consent, lesson.Stopped, lesson.ExpiresAt, now) is { } blocked)
        {
            return blocked;
        }

        if (!ChatLessonPlan.WindowOpen(inbound.Timestamp, now))
        {
            return ChatOutcome.WindowClosed;
        }

        if (wording.For(lesson.Language) is not { } words)
        {
            return ChatOutcome.Ignored;
        }

        var plan = ChatLessonPlan.Lesson(lesson, token, words, options.Value.ChatLesson.JoinDisclosure);
        await SendAsync(inbound, plan, eventId, cancellationToken);
        return ChatOutcome.LessonSent;
    }

    private async Task<string> PictureAsync(
        InboundMessage inbound, string token, string choice, string eventId, CancellationToken cancellationToken)
    {
        var (lesson, problem) = await FindAsync(inbound, token, cancellationToken);
        if (lesson is null)
        {
            return problem;
        }

        if (lesson is not { Todays.Picture: { } picture, PictureAnswer: not null })
        {
            return ChatOutcome.UnknownToken;
        }

        if (!ChatLessonPlan.Choices(picture).Contains(choice))
        {
            return ChatOutcome.BadChoice;
        }

        // As the family page: a stopped family or an expired link takes no answer.
        var now = clock.GetUtcNow();
        if (lesson.Stopped || now >= lesson.ExpiresAt)
        {
            return lesson.Stopped ? "stopped" : "expired";
        }

        var after = await store.SavePictureAnswerAsync(token, EventGuid("picture", inbound.Id), choice, now, cancellationToken);
        if (wording.For(lesson.Language) is { } words)
        {
            await SendAsync(
                inbound, [new ChatText(ChatLessonPlan.PictureReply(words, lesson.ChildName, choice, after))], eventId, cancellationToken);
        }

        return ChatOutcome.PictureSaved;
    }

    private async Task<string> ReasonAsync(
        InboundMessage inbound, string token, string category, string eventId, CancellationToken cancellationToken)
    {
        var (lesson, problem) = await FindAsync(inbound, token, cancellationToken);
        if (lesson is null)
        {
            return problem;
        }

        if (!ReasonCategory.All.Contains(category))
        {
            return ChatOutcome.BadChoice;
        }

        var now = clock.GetUtcNow();
        if (lesson.Stopped || now >= lesson.ExpiresAt)
        {
            return lesson.Stopped ? "stopped" : "expired";
        }

        await store.SaveReasonAsync(lesson, category, EventGuid("reason", inbound.Id), now, cancellationToken);
        if (wording.For(lesson.Language) is { } words)
        {
            await SendAsync(inbound, [new ChatText(words.ReasonThanks)], eventId, cancellationToken);
        }

        return ChatOutcome.ReasonSaved;
    }

    /// <summary>
    /// STOP (FR-19): stops every child this number is saved for, even after the window closed; one reply, in their
    /// languages, while it is open.
    /// </summary>
    private async Task<string> StopAsync(InboundMessage inbound, string eventId, CancellationToken cancellationToken)
    {
        var children = await store.FindByPhoneAsync("+" + inbound.From, cancellationToken);
        if (children.Count == 0)
        {
            return ChatOutcome.UnknownSender;
        }

        var now = clock.GetUtcNow();
        foreach (var child in children)
        {
            await store.SetStoppedAsync(child.Id, now, cancellationToken);
        }

        var lines = children.Select(c => c.Language).Distinct().Select(l => wording.For(l)?.Stopped).OfType<string>().ToList();
        if (lines.Count > 0)
        {
            await SendAsync(inbound, [new ChatText(string.Join('\n', lines))], eventId, cancellationToken);
        }

        return ChatOutcome.StopSaved;
    }

    /// <summary>The lesson behind the token, if the message came from one of the family's saved numbers.</summary>
    private async Task<(ChatLesson? Lesson, string Problem)> FindAsync(
        InboundMessage inbound, string token, CancellationToken cancellationToken)
    {
        if (await store.FindLessonAsync(token, cancellationToken) is not { } lesson)
        {
            return (null, ChatOutcome.UnknownToken);
        }

        return lesson.Phones.Contains("+" + inbound.From)
            ? (lesson, "")
            : (null, ChatOutcome.UnknownSender);
    }

    /// <summary>
    /// Sends in order, each only while the window opened by <paramref name="inbound"/> is still open. One that fails is
    /// logged and the rest still go: the text carries the lesson even if a voice note doesn't.
    /// </summary>
    private async Task SendAsync(
        InboundMessage inbound, IReadOnlyList<ChatMessage> messages, string eventId, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            if (!ChatLessonPlan.WindowOpen(inbound.Timestamp, clock.GetUtcNow()))
            {
                logger.LogInformation("WhatsApp event {EventId}: window closed, rest not sent", eventId);
                return;
            }

            var result = message is ChatVoice voice
                ? await VoiceAsync(inbound.From, voice, cancellationToken)
                : await client.SendChatAsync(inbound.From, message, cancellationToken);
            logger.LogInformation(
                "WhatsApp event {EventId}: {Kind} {Outcome} {Error}", eventId, message.GetType().Name, result.Outcome, result.Error);
        }
    }

    private async Task<ChannelResult> VoiceAsync(string to, ChatVoice voice, CancellationToken cancellationToken)
    {
        if (media.Get(voice.Key, clock.GetUtcNow()) is not { } mediaId)
        {
            List<byte[]> clips = [];
            foreach (var id in voice.ClipIds)
            {
                // Voice notes are Ogg Opus: every clip we store is (greetings, disclosure, lesson audio).
                if (await store.GetClipAsync(id, cancellationToken) is not { } clip
                    || !clip.ContentType.StartsWith(OpusEncoding.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    return ChannelResult.Skipped("no-clip");
                }

                clips.Add(clip.Bytes);
            }

            var bytes = clips.Count == 1 ? clips[0] : OpusEncoding.Join(clips).Ogg;
            var upload = await client.UploadMediaAsync(bytes, OpusEncoding.ContentType, cancellationToken);
            if (upload is not { Outcome: DeliveryStatus.Sent, ProviderMessageId: { } uploaded })
            {
                return upload;
            }

            media.Set(voice.Key, uploaded, clock.GetUtcNow());
            mediaId = uploaded;
        }

        return await client.SendChatAsync(to, new ChatAudio(mediaId), cancellationToken);
    }
}
