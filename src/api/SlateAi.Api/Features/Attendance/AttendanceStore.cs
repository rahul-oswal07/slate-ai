using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance.Source;

namespace SlateAi.Api.Features.Attendance;

/// <summary>Database side of attendance, kept behind an interface so endpoint tests can use a fake.</summary>
public interface IAttendanceStore
{
    /// <summary>The class's children by roll number, or null if the class doesn't exist.</summary>
    Task<IReadOnlyList<RosterChild>?> GetRosterAsync(Guid classId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttendanceRow>> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>
    /// Applies the teacher's register (<see cref="RegisterMerge.FromTeacher"/>) unless
    /// <paramref name="clientRequestId"/> was already applied, then returns the stored counts for that date.
    /// </summary>
    Task<MarkAttendanceResponse> SaveRegisterAsync(
        Guid classId,
        Guid clientRequestId,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        IReadOnlySet<Guid> absent,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a SATS register (<see cref="RegisterMerge.FromSource"/>) unless one is already stored for the
    /// date, and returns the day's rows.
    /// </summary>
    Task<IReadOnlyList<AttendanceRow>> SaveSourceRegisterAsync(
        Guid classId,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        SourceResult.Arrived register,
        CancellationToken cancellationToken);

    /// <summary>Deletes the date's rows (demo "SATS not in yet").</summary>
    Task RemoveDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);
}

public sealed class AttendanceStore(AppDbContext db, TimeProvider clock) : IAttendanceStore
{
    private const string EndpointName = "attendance";

    public async Task<IReadOnlyList<RosterChild>?> GetRosterAsync(Guid classId, CancellationToken cancellationToken)
    {
        if (!await db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            return null;
        }

        return await db.Children
            .Where(c => c.ClassId == classId)
            .OrderBy(c => c.RollNo)
            .Select(c => new RosterChild(c.Id, c.RollNo, c.FirstName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRow>> GetDayAsync(
        Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        await db.Attendance
            .Where(a => a.ClassId == classId && a.Date == date)
            .Select(a => new AttendanceRow(a.ChildId, a.Present, a.Source, a.SourcePresent, a.ReceivedAt, a.CorrectedAt))
            .ToListAsync(cancellationToken);

    public async Task<MarkAttendanceResponse> SaveRegisterAsync(
        Guid classId,
        Guid clientRequestId,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        IReadOnlySet<Guid> absent,
        CancellationToken cancellationToken)
    {
        await using (var transaction = await LockDayAsync(classId, date, cancellationToken))
        {
            if (!await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
            {
                var existing = await GetDayAsync(classId, date, cancellationToken);
                await ApplyAsync(classId, date, RegisterMerge.FromTeacher(existing, childIds, absent, clock.GetUtcNow()),
                    cancellationToken);
                db.ProcessedRequests.Add(new ProcessedRequest
                {
                    ClientRequestId = clientRequestId,
                    Endpoint = EndpointName,
                    CreatedAt = clock.GetUtcNow(),
                });
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

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

    public async Task<IReadOnlyList<AttendanceRow>> SaveSourceRegisterAsync(
        Guid classId,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        SourceResult.Arrived register,
        CancellationToken cancellationToken)
    {
        await using var transaction = await LockDayAsync(classId, date, cancellationToken);

        // Checked under the lock, so two requests arriving together store the register once.
        var existing = await GetDayAsync(classId, date, cancellationToken);
        if (existing.Any(r => r.SourcePresent is not null))
        {
            return existing;
        }

        var merged = RegisterMerge.FromSource(existing, childIds, register.AbsentChildIds, register.ReceivedAt);
        await ApplyAsync(classId, date, merged, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return merged;
    }

    public Task RemoveDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        db.Attendance.Where(a => a.ClassId == classId && a.Date == date).ExecuteDeleteAsync(cancellationToken);

    /// <summary>
    /// Serialises writes per class and date, so a replay racing its original, two corrections, or two
    /// SATS fetches can't interleave.
    /// </summary>
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> LockDayAsync(
        Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = $"attendance:{classId}:{date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);
        return transaction;
    }

    private async Task ApplyAsync(
        Guid classId, DateOnly date, IReadOnlyList<AttendanceRow> rows, CancellationToken cancellationToken)
    {
        var tracked = await db.Attendance
            .Where(a => a.ClassId == classId && a.Date == date)
            .ToDictionaryAsync(a => a.ChildId, cancellationToken);
        foreach (var row in rows)
        {
            if (!tracked.TryGetValue(row.ChildId, out var entity))
            {
                entity = new Data.Entities.Attendance { ClassId = classId, Date = date, ChildId = row.ChildId };
                db.Attendance.Add(entity);
            }

            entity.Present = row.Present;
            entity.Source = row.Source;
            entity.SourcePresent = row.SourcePresent;
            // Npgsql stores timestamptz as UTC only; sources may report IST.
            entity.ReceivedAt = row.ReceivedAt?.ToUniversalTime();
            entity.CorrectedAt = row.CorrectedAt?.ToUniversalTime();
        }
    }
}
