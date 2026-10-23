using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Buddy;

/// <summary>A tick on a card; null leaves that tick as it is.</summary>
public sealed record BuddyTickChange(bool? HomeworkReturned, bool? Done);

/// <summary>Database side of buddy cards, behind an interface so endpoint tests can use a fake.</summary>
public interface IBuddyStore
{
    /// <summary>The class's catch-up targets per child and week (the ledger rows come from <c>ILedgerStore</c>).</summary>
    Task<IReadOnlyList<CatchUpTargets>> GetTargetsAsync(Guid classId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BuddyTicks>> GetTicksAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>Applies the change; a replayed <paramref name="clientRequestId"/> changes nothing.</summary>
    Task SaveTicksAsync(
        Guid classId, Guid childId, DateOnly date, BuddyTickChange change, Guid clientRequestId, DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed class BuddyStore(AppDbContext db) : IBuddyStore
{
    public async Task<IReadOnlyList<CatchUpTargets>> GetTargetsAsync(Guid classId, CancellationToken cancellationToken) =>
        await (
                from a in db.CatchUpAssignments
                where a.ClassId == classId
                join p in db.CatchUpPackages on a.PackageId equals p.Id
                select new CatchUpTargets(a.ChildId, a.WeekOf, p.TargetCompetencyIds))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BuddyTicks>> GetTicksAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        await db.BuddyCards
            .Where(c => c.ClassId == classId && c.Date == date)
            .Select(c => new BuddyTicks(c.ChildId, c.HomeworkReturnedAt != null, c.DoneAt != null))
            .ToListAsync(cancellationToken);

    public async Task SaveTicksAsync(
        Guid classId, Guid childId, DateOnly date, BuddyTickChange change, Guid clientRequestId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // A double tap or a retry racing its original must not trip the unique (ChildId, Date) index.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"buddy:{childId}:{date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return;
        }

        var card = await db.BuddyCards.SingleOrDefaultAsync(c => c.ChildId == childId && c.Date == date, cancellationToken);
        if (card is null)
        {
            card = new BuddyCard { Id = Guid.NewGuid(), ClassId = classId, ChildId = childId, Date = date };
            db.BuddyCards.Add(card);
        }

        if (change.HomeworkReturned is { } returned)
        {
            card.HomeworkReturnedAt = returned ? card.HomeworkReturnedAt ?? now : null;
        }

        if (change.Done is { } done)
        {
            card.DoneAt = done ? card.DoneAt ?? now : null;
        }

        card.UpdatedAt = now;
        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = "buddy-card", CreatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
