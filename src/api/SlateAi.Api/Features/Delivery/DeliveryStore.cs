using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

public sealed class DeliveryStore(AppDbContext db) : IDeliveryStore
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
                    d.Id, d.ChildId, d.Attempts, c.Consent, Stopped = c.ParentStoppedAt != null, c.FirstName,
                    m.Language, m.SlotValues, m.Token, m.ExpiresAt, t.Version,
                })
            .Take(limit)
            .ToListAsync(cancellationToken);

        var catchUps = await (
                from d in db.ParentDeliveries.AsNoTracking()
                where d.Status == DeliveryStatus.Pending && d.CatchUpAssignmentId != null
                join a in db.CatchUpAssignments on d.CatchUpAssignmentId equals a.Id
                join p in db.CatchUpPackages on a.PackageId equals p.Id
                join c in db.Children on d.ChildId equals c.Id
                join k in db.Classes on a.ClassId equals k.Id
                join t in db.Teachers on k.TeacherId equals t.Id
                where a.Token != null
                orderby d.CreatedAt
                select new
                {
                    d.Id, d.ChildId, d.Attempts, c.Consent, Stopped = c.ParentStoppedAt != null, c.FirstName,
                    p.Language, a.Token, a.ExpiresAt, Teacher = t.DisplayName,
                })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return
        [
            .. evening.Select(e =>
            {
                var slots = JsonSerializer.Deserialize<SlotValues>(e.SlotValues, Json)!;
                return new PendingDelivery(e.Id, e.Attempts, e.Consent, e.Stopped, e.ExpiresAt, new DeliveryMessage(
                    e.Id, e.ChildId, DeliveryKinds.Evening, e.Language, [e.FirstName, slots.Lesson, slots.Page], e.Token, e.Version));
            }),
            .. catchUps.Select(c => new PendingDelivery(c.Id, c.Attempts, c.Consent, c.Stopped, c.ExpiresAt, new DeliveryMessage(
                c.Id, c.ChildId, DeliveryKinds.CatchUp, c.Language, [c.FirstName, c.Teacher], c.Token!))),
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

    /// <summary>A pending delivery row for a message just stored; added by the senders' stores in their transaction.</summary>
    public static ParentDelivery Queue(Guid classId, Guid childId, DateTimeOffset now, Guid? eveningMessageId = null, Guid? assignmentId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            ChildId = childId,
            EveningMessageId = eveningMessageId,
            CatchUpAssignmentId = assignmentId,
            Status = DeliveryStatus.Pending,
            CreatedAt = now.ToUniversalTime(),
        };
}
