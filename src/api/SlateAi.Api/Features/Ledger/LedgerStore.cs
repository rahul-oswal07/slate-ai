using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;

namespace SlateAi.Api.Features.Ledger;

public sealed record LedgerChildRow(Guid Id, int RollNo, string FirstName);

public sealed record RegisterRow(Guid ChildId, DateOnly Date, bool Present);

public sealed record LessonRow(Guid Id, DateOnly Date, IReadOnlyList<string> CompetencyIds);

/// <summary>Everything the ledger is computed from, as plain rows.</summary>
public sealed record LedgerData(
    IReadOnlyList<LedgerChildRow> Children,
    IReadOnlyList<RegisterRow> Register,
    IReadOnlyList<LessonRow> Lessons);

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
            .Select(c => new LedgerChildRow(c.Id, c.RollNo, c.FirstName))
            .ToListAsync(cancellationToken);
        var register = await db.Attendance
            .Where(a => a.ClassId == classId)
            .Select(a => new RegisterRow(a.ChildId, a.Date, a.Present))
            .ToListAsync(cancellationToken);
        var lessons = await db.Lessons
            .Where(l => l.ClassId == classId)
            .Select(l => new LessonRow(l.Id, l.Date, l.CompetencyIds))
            .ToListAsync(cancellationToken);
        return new LedgerData(children, register, lessons);
    }
}
