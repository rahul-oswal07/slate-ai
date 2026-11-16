using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Delivery;

namespace SlateAi.Api.Features.WeeklyStar;

public sealed record NewWeeklyStar(
    Guid ChildId, string Language, string Version, string MessageText, string BadgeText, string Token,
    DateTimeOffset SentAt, DateTimeOffset ExpiresAt);

/// <summary>What the star page needs, found by its token. The texts still have {child} in them.</summary>
public sealed record WeeklyStarPageRecord(
    string FirstName, string TeacherName, string Language, DateOnly WeekOf, string MessageText, string BadgeText,
    DateTimeOffset ExpiresAt, bool Stopped);

/// <summary>A star message sent to one child's family, for the demo's family phone.</summary>
public sealed record SentWeeklyStar(Guid ChildId, DateOnly WeekOf, DateTimeOffset SentAt, string Token);

/// <summary>Database side of the weekly star (T-125, D-118), behind an interface so endpoints are tested with a fake.</summary>
public interface IWeeklyStarStore
{
    /// <summary>The wording version she approved for this class, or null.</summary>
    Task<string?> GetApprovedVersionAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>Records her approval of <paramref name="version"/>. Approving again is a no-op.</summary>
    Task ApproveAsync(Guid classId, string version, Guid teacherId, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetSentChildIdsAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the messages and queues their deliveries in one transaction, skipping children already sent one for the
    /// week. Adds nothing unless she still has the messages' version approved (guardrail 3). Returns how many were added.
    /// </summary>
    Task<int> AddAsync(Guid classId, DateOnly weekOf, IReadOnlyList<NewWeeklyStar> messages, CancellationToken cancellationToken);

    Task<WeeklyStarPageRecord?> FindByTokenAsync(string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<SentWeeklyStar>> GetSentAsync(Guid classId, CancellationToken cancellationToken);
}

public sealed class WeeklyStarStore(AppDbContext db, TimeProvider clock) : IWeeklyStarStore
{
    public Task<string?> GetApprovedVersionAsync(Guid classId, CancellationToken cancellationToken) =>
        db.WeeklyStarApprovals.Where(a => a.ClassId == classId).Select(a => a.Version).SingleOrDefaultAsync(cancellationToken);

    public async Task ApproveAsync(Guid classId, string version, Guid teacherId, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync($"weekly-star-approval:{classId}", cancellationToken);
        var row = await db.WeeklyStarApprovals.SingleOrDefaultAsync(a => a.ClassId == classId, cancellationToken);
        if (row?.Version == version)
        {
            return;
        }

        if (row is null)
        {
            row = new WeeklyStarApproval { ClassId = classId, Version = version };
            db.WeeklyStarApprovals.Add(row);
        }

        row.Version = version;
        row.ApprovedAt = clock.GetUtcNow();
        row.ApprovedByTeacherId = teacherId;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetSentChildIdsAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken) =>
        (await db.WeeklyStarMessages
            .Where(m => m.ClassId == classId && m.WeekOf == weekOf)
            .Select(m => m.ChildId)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    public async Task<int> AddAsync(
        Guid classId, DateOnly weekOf, IReadOnlyList<NewWeeklyStar> messages, CancellationToken cancellationToken)
    {
        // The 6 pm job and a demo send can overlap; the lock plus the (ChildId, WeekOf) index keep it to one each.
        await using var transaction = await LockAsync($"weekly-star:{classId}:{weekOf:yyyy-MM-dd}", cancellationToken);
        var approved = await GetApprovedVersionAsync(classId, cancellationToken);
        var sent = await GetSentChildIdsAsync(classId, weekOf, cancellationToken);
        var now = clock.GetUtcNow();
        var added = 0;
        foreach (var message in messages.Where(m => m.Version == approved && !sent.Contains(m.ChildId)))
        {
            var id = Guid.NewGuid();
            db.WeeklyStarMessages.Add(new WeeklyStarMessage
            {
                Id = id,
                ClassId = classId,
                ChildId = message.ChildId,
                WeekOf = weekOf,
                Language = message.Language,
                Version = message.Version,
                MessageText = message.MessageText,
                BadgeText = message.BadgeText,
                Token = message.Token,
                SentAt = message.SentAt.ToUniversalTime(),
                ExpiresAt = message.ExpiresAt.ToUniversalTime(),
            });
            // Same transaction as the message: only what passed guardrail 3 is ever queued for a phone (T-40).
            db.ParentDeliveries.Add(DeliveryStore.Queue(classId, message.ChildId, now, weeklyStarMessageId: id));
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return added;
    }

    public Task<WeeklyStarPageRecord?> FindByTokenAsync(string token, CancellationToken cancellationToken) =>
        (from m in db.WeeklyStarMessages
         where m.Token == token
         join c in db.Children on m.ChildId equals c.Id
         join k in db.Classes on m.ClassId equals k.Id
         join t in db.Teachers on k.TeacherId equals t.Id
         select new WeeklyStarPageRecord(
             c.FirstName, t.DisplayName, m.Language, m.WeekOf, m.MessageText, m.BadgeText, m.ExpiresAt, c.ParentStoppedAt != null))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<SentWeeklyStar>> GetSentAsync(Guid classId, CancellationToken cancellationToken) =>
        await db.WeeklyStarMessages
            .Where(m => m.ClassId == classId)
            .Select(m => new SentWeeklyStar(m.ChildId, m.WeekOf, m.SentAt, m.Token))
            .ToListAsync(cancellationToken);

    private async Task<IDbContextTransaction> LockAsync(string key, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        return transaction;
    }
}
