using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.Delivery;

/// <summary>A queued delivery with what the send rules need (consent, stop, link expiry) and the message itself.</summary>
public sealed record PendingDelivery(
    Guid Id,
    int Attempts,
    string Consent,
    bool Stopped,
    DateTimeOffset? ExpiresAt,
    DeliveryMessage Message);

/// <param name="Status">A <see cref="DeliveryStatus"/> value.</param>
public sealed record DeliveryUpdate(
    Guid Id, string Status, string Channel, string? ProviderMessageId, string? Error, DateTimeOffset? SentAt);

/// <summary>Database side of the delivery outbox, behind an interface so the dispatcher can be tested with a fake.</summary>
public interface IDeliveryStore
{
    /// <summary>Oldest pending deliveries first, at most <paramref name="limit"/> of each kind.</summary>
    Task<IReadOnlyList<PendingDelivery>> GetPendingAsync(int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Takes the delivery for one attempt: true only if it is still pending with <paramref name="attempts"/> tries, so two
    /// dispatchers (the 6 pm job and a demo send) never send the same message.
    /// </summary>
    Task<bool> TryClaimAsync(Guid id, int attempts, CancellationToken cancellationToken);

    Task MarkAsync(DeliveryUpdate update, CancellationToken cancellationToken);
}

public sealed class DeliveryStore(AppDbContext db, ICurriculumProvider curricula) : IDeliveryStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PendingDelivery>> GetPendingAsync(int limit, CancellationToken cancellationToken)
    {
        var evening = await (
                from d in db.ParentDeliveries.AsNoTracking()
                where d.Status == DeliveryStatus.Pending && d.EveningMessageId != null
                join m in db.EveningMessages on d.EveningMessageId equals m.Id
                join t in db.MessageTemplates on m.TemplateId equals t.Id
                join c in db.Children on d.ChildId equals c.Id
                orderby d.CreatedAt
                select new
                {
                    d.Id, d.ChildId, d.ClassId, d.Attempts, c.Consent, Stopped = c.ParentStoppedAt != null, c.FirstName,
                    m.Language, m.SlotValues, m.Token, m.ExpiresAt, t.Version, m.TodaysMessageId, d.Recipient,
                    // Only a number saved on the admin page is dialled; seeded ones are fictional (T-118, D-110). Each row
                    // has its own number, the family's 1 or 2 (T-140).
                    Phone = d.Recipient == 2
                        ? (c.ParentPhone2SetAt != null ? c.ParentPhone2 : null)
                        : (c.ParentPhoneSetAt != null ? c.ParentPhone : null),
                })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var catchUps = await (
                from d in db.ParentDeliveries.AsNoTracking()
                where d.Status == DeliveryStatus.Pending && d.CatchUpAssignmentId != null
                join a in db.CatchUpAssignments on d.CatchUpAssignmentId equals a.Id
                join p in db.CatchUpPackages on a.PackageId equals p.Id
                join c in db.Children on d.ChildId equals c.Id
                where a.Token != null
                orderby d.CreatedAt
                select new
                {
                    d.Id, d.ChildId, d.ClassId, d.Attempts, c.Consent, Stopped = c.ParentStoppedAt != null, c.FirstName,
                    p.Language, a.Token, a.ExpiresAt, p.TargetCompetencyIds, d.Recipient,
                    Phone = d.Recipient == 2
                        ? (c.ParentPhone2SetAt != null ? c.ParentPhone2 : null)
                        : (c.ParentPhoneSetAt != null ? c.ParentPhone : null),
                })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var stars = await (
                from d in db.ParentDeliveries.AsNoTracking()
                where d.Status == DeliveryStatus.Pending && d.WeeklyStarMessageId != null
                join m in db.WeeklyStarMessages on d.WeeklyStarMessageId equals m.Id
                join c in db.Children on d.ChildId equals c.Id
                orderby d.CreatedAt
                select new
                {
                    d.Id, d.ChildId, d.Attempts, c.Consent, Stopped = c.ParentStoppedAt != null, c.FirstName,
                    m.Language, m.Token, m.ExpiresAt, m.Version, d.Recipient,
                    Phone = d.Recipient == 2
                        ? (c.ParentPhone2SetAt != null ? c.ParentPhone2 : null)
                        : (c.ParentPhoneSetAt != null ? c.ParentPhone : null),
                })
            .Take(limit)
            .ToListAsync(cancellationToken);

        // The lessons of today's messages these evening rows carry: the template names them (T-104 a).
        var todaysIds = evening.Where(e => e.TodaysMessageId != null).Select(e => e.TodaysMessageId!.Value).Distinct().ToList();
        var todaysLessons = todaysIds.Count == 0
            ? []
            : await db.TodaysMessages.AsNoTracking()
                .Where(t => todaysIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.CompetencyIds, cancellationToken);

        // A batch mixes classes, and each names lessons from its own curriculum (T-145).
        var curriculumOf = new Dictionary<Guid, CurriculumGraph>();
        foreach (var classId in evening.Select(e => e.ClassId).Concat(catchUps.Select(c => c.ClassId)).Distinct())
        {
            curriculumOf[classId] = await curricula.ForClassAsync(classId, cancellationToken);
        }

        return
        [
            .. evening.Select(e =>
            {
                if (e.TodaysMessageId is { } todaysId)
                {
                    // Today's message (D-91): child, lesson and page built by code; empty if they can't be (not sent).
                    var lessons = todaysLessons.GetValueOrDefault(todaysId) ?? [];
                    return new PendingDelivery(e.Id, e.Attempts, e.Consent, e.Stopped, e.ExpiresAt, new DeliveryMessage(
                        e.Id, e.ChildId, DeliveryKinds.Todays, e.Language,
                        TemplateValues.Todays(e.FirstName, e.Language, lessons, curriculumOf[e.ClassId]) ?? [], e.Token, e.Version, e.Phone, e.Recipient));
                }

                var slots = JsonSerializer.Deserialize<SlotValues>(e.SlotValues, Json)!;
                return new PendingDelivery(e.Id, e.Attempts, e.Consent, e.Stopped, e.ExpiresAt, new DeliveryMessage(
                    e.Id, e.ChildId, DeliveryKinds.Evening, e.Language, [e.FirstName, slots.Lesson, slots.Page], e.Token, e.Version, e.Phone, e.Recipient));
            }),
            .. catchUps.Select(c => new PendingDelivery(c.Id, c.Attempts, c.Consent, c.Stopped, c.ExpiresAt, new DeliveryMessage(
                c.Id, c.ChildId, DeliveryKinds.CatchUp, c.Language,
                TemplateValues.CatchUp(c.FirstName, c.Language, c.TargetCompetencyIds, curriculumOf[c.ClassId]) ?? [], c.Token!, Phone: c.Phone, Recipient: c.Recipient))),
            // weekly_star_v1 (T-125): {{1}} is the child's name, nothing else.
            .. stars.Select(s => new PendingDelivery(s.Id, s.Attempts, s.Consent, s.Stopped, s.ExpiresAt, new DeliveryMessage(
                s.Id, s.ChildId, DeliveryKinds.WeeklyStar, s.Language, [s.FirstName], s.Token, s.Version, s.Phone, s.Recipient))),
        ];
    }

    public async Task<bool> TryClaimAsync(Guid id, int attempts, CancellationToken cancellationToken) =>
        await db.ParentDeliveries
            .Where(d => d.Id == id && d.Status == DeliveryStatus.Pending && d.Attempts == attempts)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Attempts, attempts + 1), cancellationToken) == 1;

    public Task MarkAsync(DeliveryUpdate update, CancellationToken cancellationToken) =>
        db.ParentDeliveries
            .Where(d => d.Id == update.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, update.Status)
                .SetProperty(d => d.Channel, update.Channel)
                .SetProperty(d => d.ProviderMessageId, update.ProviderMessageId)
                .SetProperty(d => d.LastError, update.Error)
                .SetProperty(d => d.SentAt, update.SentAt), cancellationToken);

    /// <summary>The family's numbers a message goes to (T-140, D-122): each has its own row.</summary>
    public static readonly short[] Recipients = [1, 2];

    /// <summary>
    /// The pending delivery rows for a message just stored, one per family number (T-140, D-122); added by the senders'
    /// stores in their transaction. A number not saved by send time is skipped then (<c>no-recipient</c>).
    /// </summary>
    public static IEnumerable<ParentDelivery> QueueAll(
        Guid classId, Guid childId, DateTimeOffset now, Guid? eveningMessageId = null, Guid? assignmentId = null,
        Guid? weeklyStarMessageId = null) =>
        Recipients.Select(r => Queue(classId, childId, now, eveningMessageId, assignmentId, weeklyStarMessageId, r));

    /// <summary>One pending delivery row, for the family's number <paramref name="recipient"/>.</summary>
    public static ParentDelivery Queue(
        Guid classId, Guid childId, DateTimeOffset now, Guid? eveningMessageId = null, Guid? assignmentId = null,
        Guid? weeklyStarMessageId = null, short recipient = 1) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            ChildId = childId,
            Recipient = recipient,
            EveningMessageId = eveningMessageId,
            CatchUpAssignmentId = assignmentId,
            WeeklyStarMessageId = weeklyStarMessageId,
            Status = DeliveryStatus.Pending,
            CreatedAt = now.ToUniversalTime(),
        };
}
