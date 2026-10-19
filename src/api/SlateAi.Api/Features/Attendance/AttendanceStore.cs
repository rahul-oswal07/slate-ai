using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Attendance;

/// <summary>Database side of attendance, kept behind an interface so endpoint tests can use a fake.</summary>
public interface IAttendanceStore
{
    /// <summary>The class's children, or null if the class doesn't exist.</summary>
    Task<IReadOnlyList<Guid>?> GetChildIdsAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the register for the date unless <paramref name="clientRequestId"/> was already applied,
    /// then returns the stored counts for that date.
    /// </summary>
    Task<MarkAttendanceResponse> SaveRegisterAsync(
        Guid classId,
        Guid clientRequestId,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        IReadOnlySet<Guid> absent,
        CancellationToken cancellationToken);
}

public sealed class AttendanceStore(AppDbContext db, TimeProvider clock) : IAttendanceStore
{
    private const string EndpointName = "attendance";

    public async Task<IReadOnlyList<Guid>?> GetChildIdsAsync(Guid classId, CancellationToken cancellationToken)
    {
        if (!await db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            return null;
        }

        return await db.Children
            .Where(c => c.ClassId == classId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<MarkAttendanceResponse> SaveRegisterAsync(
        Guid classId,
        Guid clientRequestId,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        IReadOnlySet<Guid> absent,
        CancellationToken cancellationToken)
    {
        // Serialise writes per class and date so a replay racing its original, or two corrections, can't interleave.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = $"attendance:{classId}:{date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);

        if (!await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            await db.Attendance
                .Where(a => a.ClassId == classId && a.Date == date)
                .ExecuteDeleteAsync(cancellationToken);

            db.Attendance.AddRange(childIds.Select(id => new Data.Entities.Attendance
            {
                ClassId = classId,
                Date = date,
                ChildId = id,
                Present = !absent.Contains(id),
            }));
            db.ProcessedRequests.Add(new ProcessedRequest
            {
                ClientRequestId = clientRequestId,
                Endpoint = EndpointName,
                CreatedAt = clock.GetUtcNow(),
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var counts = await db.Attendance
            .Where(a => a.ClassId == classId && a.Date == date)
            .GroupBy(a => a.Present)
            .Select(g => new { Present = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return new MarkAttendanceResponse(
            date,
            Present: counts.Where(c => c.Present).Sum(c => c.Count),
            Absent: counts.Where(c => !c.Present).Sum(c => c.Count));
    }
}
