using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps;

namespace SlateAi.Api.Features.Parent;

/// <summary>A catch-up link as stored: the assignment, its package and the family's state.</summary>
/// <param name="ChildName">Written text only (the page header, "Well done"); never sent to an AI provider (D-50).</param>
public sealed record ParentCatchUpRecord(
    Guid AssignmentId,
    Guid ChildId,
    string ChildName,
    string TeacherName,
    string Language,
    string Status,
    int ScriptVersion,
    int? ApprovedScriptVersion,
    IReadOnlyList<string> TargetIds,
    string? Script,
    IReadOnlyList<string> KeyWordsKn,
    ActivityData Activity,
    CheckData Check,
    AudioSegments? Audio,
    DateTimeOffset ExpiresAt,
    bool Stopped,
    bool Answered);

/// <summary>An evening-message link: the child and day it is about, and the reason already given.</summary>
public sealed record EveningLink(Guid ChildId, Guid ClassId, DateOnly Date, DateTimeOffset ExpiresAt, bool Stopped, string? Reason);

/// <summary>Database side of the parent page, behind an interface so endpoint tests can use a fake.</summary>
public interface IParentStore
{
    /// <summary>The catch-up behind a link token, or null when the token isn't a catch-up link.</summary>
    Task<ParentCatchUpRecord?> FindCatchUpAsync(string token, CancellationToken cancellationToken);

    /// <summary>The child behind any parent link: a catch-up or an evening message, expired or not.</summary>
    Task<Guid?> FindChildAsync(string token, CancellationToken cancellationToken);

    /// <summary>Stores the first answer for the assignment.</summary>
    /// <returns>False when the assignment was already answered (the first answer counts, D-62).</returns>
    Task<bool> SaveAnswerAsync(CheckResponse response, CancellationToken cancellationToken);

    /// <summary>The evening message behind a link token, or null when the token isn't an evening link.</summary>
    Task<EveningLink?> FindEveningAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the reason for (ChildId, Date); a later one replaces it (the family may correct a tap). A replayed
    /// <paramref name="clientRequestId"/> changes nothing, even after a newer tap (<c>ProcessedRequests</c>).
    /// </summary>
    Task SaveReasonAsync(AbsenceReason reason, Guid clientRequestId, CancellationToken cancellationToken);

    /// <summary>Sets or clears <see cref="Child.ParentStoppedAt"/> (D-19).</summary>
    Task SetStoppedAsync(Guid childId, bool stopped, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class ParentStore(AppDbContext db) : IParentStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ParentCatchUpRecord?> FindCatchUpAsync(string token, CancellationToken cancellationToken)
    {
        var row = await (
                from a in db.CatchUpAssignments.AsNoTracking()
                where a.Token == token
                join p in db.CatchUpPackages on a.PackageId equals p.Id
                join c in db.Children on a.ChildId equals c.Id
                join k in db.Classes on a.ClassId equals k.Id
                join t in db.Teachers on k.TeacherId equals t.Id
                select new
                {
                    a.Id, a.ChildId, c.FirstName, t.DisplayName, p.Language, p.Status, p.ScriptVersion,
                    p.ApprovedScriptVersion, p.TargetCompetencyIds, p.Script, p.KeyWordsKn, p.Activity, p.CheckItem,
                    p.AudioSegments, a.ExpiresAt, Stopped = c.ParentStoppedAt != null,
                    Answered = db.CheckResponses.Any(r => r.AssignmentId == a.Id),
                })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new ParentCatchUpRecord(
                row.Id, row.ChildId, row.FirstName, row.DisplayName, row.Language, row.Status, row.ScriptVersion,
                row.ApprovedScriptVersion, row.TargetCompetencyIds, row.Script, row.KeyWordsKn,
                JsonSerializer.Deserialize<ActivityData>(row.Activity, Json)!,
                JsonSerializer.Deserialize<CheckData>(row.CheckItem, Json)!,
                row.AudioSegments is null ? null : JsonSerializer.Deserialize<AudioSegments>(row.AudioSegments, Json),
                // A token is only set when sent, together with ExpiresAt; treat a missing one as already expired.
                row.ExpiresAt ?? DateTimeOffset.MinValue,
                row.Stopped,
                row.Answered);
    }

    public async Task<Guid?> FindChildAsync(string token, CancellationToken cancellationToken) =>
        await db.CatchUpAssignments.Where(a => a.Token == token).Select(a => (Guid?)a.ChildId)
            .SingleOrDefaultAsync(cancellationToken)
        ?? await db.EveningMessages.Where(m => m.Token == token).Select(m => (Guid?)m.ChildId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> SaveAnswerAsync(CheckResponse response, CancellationToken cancellationToken)
    {
        // Two taps racing (a retry while the first is still in flight) must not both insert.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"answer:{response.AssignmentId}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        if (await db.CheckResponses.AnyAsync(r => r.AssignmentId == response.AssignmentId, cancellationToken))
        {
            return false;
        }

        db.CheckResponses.Add(response);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<EveningLink?> FindEveningAsync(string token, CancellationToken cancellationToken) =>
        (from m in db.EveningMessages.AsNoTracking()
         where m.Token == token
         join c in db.Children on m.ChildId equals c.Id
         select new EveningLink(
             m.ChildId, m.ClassId, m.Date, m.ExpiresAt, c.ParentStoppedAt != null,
             db.AbsenceReasons.Where(r => r.ChildId == m.ChildId && r.Date == m.Date).Select(r => r.Category).FirstOrDefault()))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task SaveReasonAsync(AbsenceReason reason, Guid clientRequestId, CancellationToken cancellationToken)
    {
        // A double tap or a retry racing its original must not trip the unique (ChildId, Date) index.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"reason:{reason.ChildId}:{reason.Date:yyyy-MM-dd}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return;
        }

        var stored = await db.AbsenceReasons
            .SingleOrDefaultAsync(r => r.ChildId == reason.ChildId && r.Date == reason.Date, cancellationToken);
        if (stored is null)
        {
            db.AbsenceReasons.Add(reason);
        }
        else
        {
            stored.Category = reason.Category;
            stored.UpdatedAt = reason.UpdatedAt;
        }

        db.ProcessedRequests.Add(new ProcessedRequest
        {
            ClientRequestId = clientRequestId, Endpoint = "parent-reason", CreatedAt = reason.UpdatedAt,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetStoppedAsync(Guid childId, bool stopped, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Keep the first stop time on a repeated stop.
        if (stopped)
        {
            await db.Children.Where(c => c.Id == childId && c.ParentStoppedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ParentStoppedAt, now), cancellationToken);
        }
        else
        {
            await db.Children.Where(c => c.Id == childId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ParentStoppedAt, (DateTimeOffset?)null), cancellationToken);
        }
    }
}
