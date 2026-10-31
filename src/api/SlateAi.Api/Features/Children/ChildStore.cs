using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Children;

public enum ChildDelete
{
    Deleted,

    /// <summary>This clientRequestId already deleted the child.</summary>
    Replay,

    /// <summary>No such child in this class.</summary>
    NotFound,
}

/// <summary>Database side of a child's data (FR-15), behind an interface so endpoint tests can use a fake.</summary>
public interface IChildStore
{
    /// <summary>
    /// Deletes the child and, through the database's cascades, everything about them: attendance, evening messages,
    /// catch-up assignments and their answers, and reasons. A catch-up made only for this child goes too, with its audio
    /// (T-19); one shared with classmates stays. Children have no other audio or transcripts.
    /// </summary>
    Task<ChildDelete> DeleteAsync(Guid classId, Guid childId, Guid clientRequestId, CancellationToken cancellationToken);
}

public sealed class ChildStore(AppDbContext db, TimeProvider clock) : IChildStore
{
    private const string Endpoint = "child-delete";

    public async Task<ChildDelete> DeleteAsync(Guid classId, Guid childId, Guid clientRequestId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return ChildDelete.Replay;
        }

        var packageIds = await db.CatchUpAssignments
            .Where(a => a.ChildId == childId && a.ClassId == classId)
            .Select(a => a.PackageId)
            .ToListAsync(cancellationToken);
        var deleted = await db.Children.Where(c => c.Id == childId && c.ClassId == classId).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return ChildDelete.NotFound;
        }

        // The AI log keeps no trace of them either: their ID leaves every call it tagged (T-73).
        await db.Database.ExecuteSqlAsync(
            $"""UPDATE "AiCalls" SET "ChildIds" = array_remove("ChildIds", {childId}) WHERE {childId} = ANY("ChildIds")""",
            cancellationToken);

        // The child's assignments are gone with them; a catch-up nobody else has is about them alone.
        await db.CatchUpPackages
            .Where(p => packageIds.Contains(p.Id) && !db.CatchUpAssignments.Any(a => a.PackageId == p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        db.ProcessedRequests.Add(new ProcessedRequest
        {
            ClientRequestId = clientRequestId, Endpoint = Endpoint, CreatedAt = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ChildDelete.Deleted;
    }
}
