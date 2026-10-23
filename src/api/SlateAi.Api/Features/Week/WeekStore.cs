using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Week;

/// <summary>Database side of the week's pacing decision, behind an interface so endpoint tests can use a fake.</summary>
public interface IWeekStore
{
    Task<DecisionRow?> GetDecisionAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken);

    /// <summary>Stores or replaces the decision (Change); a replayed <paramref name="clientRequestId"/> changes nothing.</summary>
    Task SaveDecisionAsync(
        Guid classId, DateOnly weekOf, DecisionRow decision, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class WeekStore(AppDbContext db) : IWeekStore
{
    public Task<DecisionRow?> GetDecisionAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken) =>
        db.PacingDecisions
            .Where(d => d.ClassId == classId && d.WeekOf == weekOf)
            .Select(d => new DecisionRow(d.LessonId, d.Decision))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SaveDecisionAsync(
        Guid classId, DateOnly weekOf, DecisionRow decision, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"pacing:{classId}:{weekOf:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return;
        }

        var stored = await db.PacingDecisions.SingleOrDefaultAsync(d => d.ClassId == classId && d.WeekOf == weekOf, cancellationToken);
        if (stored is null)
        {
            db.PacingDecisions.Add(new PacingDecision
            {
                Id = Guid.NewGuid(), ClassId = classId, WeekOf = weekOf, LessonId = decision.LessonId,
                Decision = decision.Decision, DecidedAt = now, UpdatedAt = now,
            });
        }
        else
        {
            stored.LessonId = decision.LessonId;
            stored.Decision = decision.Decision;
            stored.UpdatedAt = now;
        }

        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = "pacing-decision", CreatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
