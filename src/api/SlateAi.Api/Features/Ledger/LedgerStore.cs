using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;

namespace SlateAi.Api.Features.Ledger;

/// <param name="Stopped">The family stopped messages (FR-19): the ledger shows "stopped" until a catch-up is answered.</param>
public sealed record LedgerChildRow(Guid Id, int RollNo, string FirstName, bool Stopped = false);

public sealed record RegisterRow(Guid ChildId, DateOnly Date, bool Present);

public sealed record LessonRow(Guid Id, DateOnly Date, IReadOnlyList<string> CompetencyIds);

/// <summary>A child's catch-up for a week: whether the link went out and how the picture check was answered.</summary>
/// <param name="Outcome">"caughtUp" or "needsBuddy" once answered (FR-11), else null.</param>
public sealed record CatchUpRow(Guid ChildId, DateOnly WeekOf, IReadOnlyList<string> MissedCompetencyIds, bool Sent, string? Outcome);

/// <summary>Everything the ledger is computed from, as plain rows.</summary>
public sealed record LedgerData(
    IReadOnlyList<LedgerChildRow> Children,
    IReadOnlyList<RegisterRow> Register,
    IReadOnlyList<LessonRow> Lessons,
    IReadOnlyList<CatchUpRow>? CatchUps = null);

/// <summary>Database side of the ledger, kept behind an interface so endpoint tests can use a fake.</summary>
public interface ILedgerStore
{
    /// <summary>The class's rows, or null if the class doesn't exist.</summary>
    Task<LedgerData?> LoadAsync(Guid classId, CancellationToken cancellationToken);
}

public sealed class LedgerStore(AppDbContext db) : ILedgerStore
{
    public async Task<LedgerData?> LoadAsync(Guid classId, CancellationToken cancellationToken)
    {
        if (!await db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            return null;
        }

        var children = await db.Children
            .Where(c => c.ClassId == classId)
            .Select(c => new LedgerChildRow(c.Id, c.RollNo, c.FirstName, c.ParentStoppedAt != null))
            .ToListAsync(cancellationToken);
        var register = await db.Attendance
            .Where(a => a.ClassId == classId)
            .Select(a => new RegisterRow(a.ChildId, a.Date, a.Present))
            .ToListAsync(cancellationToken);
        var lessons = await db.Lessons
            // Only lessons the teacher confirmed count as missed (T2 §3).
            .Where(l => l.ClassId == classId && l.Status == Lessons.LessonValues.Confirmed)
            .Select(l => new LessonRow(l.Id, l.Date, l.CompetencyIds))
            .ToListAsync(cancellationToken);
        var catchUps = await (
                from a in db.CatchUpAssignments
                where a.ClassId == classId
                join r in db.CheckResponses on a.Id equals r.AssignmentId into responses
                from r in responses.DefaultIfEmpty()
                select new CatchUpRow(a.ChildId, a.WeekOf, a.MissedCompetencyIds, a.SentAt != null, r == null ? null : r.Outcome))
            .ToListAsync(cancellationToken);
        return new LedgerData(children, register, lessons, catchUps);
    }
}
