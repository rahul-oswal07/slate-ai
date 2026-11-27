using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Features.WhatsAppChat;

/// <summary>A child whose family's saved number sent a message (for STOP).</summary>
public sealed record ChatChild(Guid Id, string Language);

/// <summary>
/// Database side of the chat (T-106), behind an interface so the webhook can be tested with a fake. Reads and writes go
/// through the stores the family page uses (evening messages, parent answers and reasons, stop), so a tap in the chat
/// lands exactly where a tap on the page does and shows on the teacher's Today.
/// </summary>
public interface IWhatsAppChatStore
{
    /// <summary>Records a webhook event once (<c>ProcessedRequests</c>): false when it was already handled.</summary>
    Task<bool> TryRecordEventAsync(Guid eventId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>The evening message behind a link token with what the chat needs, or null.</summary>
    Task<ChatLesson?> FindLessonAsync(string token, CancellationToken cancellationToken);

    /// <summary>The children with this number saved on the admin page (number 1 or 2; E.164).</summary>
    Task<IReadOnlyList<ChatChild>> FindByPhoneAsync(string phone, CancellationToken cancellationToken);

    /// <summary>A stored clip's bytes and content type, or null.</summary>
    Task<(byte[] Bytes, string ContentType)?> GetClipAsync(Guid clipId, CancellationToken cancellationToken);

    /// <summary>As the family page's picture submit (T-90, D-108): the submits after it, or null when both are used.</summary>
    Task<(string? First, string? Second)?> SavePictureAnswerAsync(
        string token, Guid requestId, string choice, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>As the family page's reason (FR-21): a later one replaces it; a replayed request changes nothing.</summary>
    Task SaveReasonAsync(ChatLesson lesson, string category, Guid requestId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Sets <see cref="Child.ParentStoppedAt"/> (FR-19, D-19).</summary>
    Task SetStoppedAsync(Guid childId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class WhatsAppChatStore(
    AppDbContext db, IEveningStore evenings, IParentStore parents, IAudioStore audio, DisclosureClips disclosure) : IWhatsAppChatStore
{
    public const string Endpoint = "whatsapp-webhook";

    public async Task<bool> TryRecordEventAsync(Guid eventId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == eventId, cancellationToken))
        {
            return false;
        }

        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = eventId, Endpoint = Endpoint, CreatedAt = now });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Meta sent the same event twice at once: the other request has it.
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<ChatLesson?> FindLessonAsync(string token, CancellationToken cancellationToken)
    {
        var row = await (
                from m in db.EveningMessages.AsNoTracking()
                where m.Token == token
                join c in db.Children on m.ChildId equals c.Id
                select new
                {
                    m.ChildId, m.ClassId, m.Date, c.Consent,
                    // Only numbers saved on the admin page (D-114); the seed's are fictional.
                    Phone1 = c.ParentPhoneSetAt != null ? c.ParentPhone : null,
                    Phone2 = c.ParentPhone2SetAt != null ? c.ParentPhone2 : null,
                    // Guardrail 3, checked again: approved at its current version.
                    TodaysApproved = m.TodaysMessageId != null && db.TodaysMessages.Any(t =>
                        t.Id == m.TodaysMessageId && t.Status == TodaysMessageStatus.Approved && t.ApprovedVersion == t.Version),
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null || await evenings.FindByTokenAsync(token, cancellationToken) is not { } message)
        {
            return null;
        }

        var reason = (await parents.FindEveningAsync(token, cancellationToken))?.Reason;
        var intro = message.LessonAudio is null ? null : await disclosure.FindAsync(message.Language, cancellationToken);
        return new ChatLesson(
            row.ChildId, row.ClassId, row.Date, message.Language, message.ChildName, row.Consent, message.Stopped,
            message.ExpiresAt, new[] { row.Phone1, row.Phone2 }.OfType<string>().ToList(), row.TodaysApproved,
            message.Slots, message.TodaysMessage, message.PictureAnswer, reason, message.Greeting?.ClipId, intro?.Id,
            message.LessonAudio?.ClipId);
    }

    public async Task<IReadOnlyList<ChatChild>> FindByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        await db.Children.AsNoTracking()
            .Where(c => (c.ParentPhoneSetAt != null && c.ParentPhone == phone) || (c.ParentPhone2SetAt != null && c.ParentPhone2 == phone))
            .Select(c => new ChatChild(c.Id, c.HomeLanguage))
            .ToListAsync(cancellationToken);

    public async Task<(byte[] Bytes, string ContentType)?> GetClipAsync(Guid clipId, CancellationToken cancellationToken) =>
        await audio.GetBytesAsync(clipId, cancellationToken) is { } bytes
            ? (bytes, await audio.GetContentTypeAsync(clipId, cancellationToken) ?? "application/octet-stream")
            : null;

    public Task<(string? First, string? Second)?> SavePictureAnswerAsync(
        string token, Guid requestId, string choice, DateTimeOffset now, CancellationToken cancellationToken) =>
        evenings.SavePictureAnswerAsync(token, requestId, choice, now, cancellationToken);

    public Task SaveReasonAsync(
        ChatLesson lesson, string category, Guid requestId, DateTimeOffset now, CancellationToken cancellationToken) =>
        parents.SaveReasonAsync(
            new AbsenceReason
            {
                Id = Guid.NewGuid(),
                ClassId = lesson.ClassId,
                ChildId = lesson.ChildId,
                Date = lesson.Date,
                Category = category,
                CreatedAt = now,
                UpdatedAt = now,
            },
            requestId,
            cancellationToken);

    public Task SetStoppedAsync(Guid childId, DateTimeOffset now, CancellationToken cancellationToken) =>
        parents.SetStoppedAsync(childId, stopped: true, now, cancellationToken);
}
