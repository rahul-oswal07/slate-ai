using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;

namespace SlateAi.Api.Features.Admin;

/// <summary>One teacher as the admin teachers page sees it (T-133).</summary>
/// <param name="ClassTeacherName">Her class's main teacher, for "Shobha · Grade 3"; null when she has no class.</param>
/// <param name="ConsentAt">Her voice consent (<c>Teacher.ConsentAt</c>), the first-run gate; null: she sees the Welcome.</param>
/// <param name="GreetingSeconds">Her recorded greeting's length, or null when she has none.</param>
public sealed record AdminTeacherRow(
    Guid Id, string Name, string? ClassTeacherName, int? Grade, DateTimeOffset? ConsentAt, double? GreetingSeconds);

/// <summary>Database side of the admin teachers page (T-133), behind an interface so endpoint tests can use a fake.</summary>
public interface IAdminTeacherStore
{
    /// <summary>Every teacher, by name.</summary>
    Task<IReadOnlyList<AdminTeacherRow>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Clears her voice consent and deletes her greeting (D-44), unless <paramref name="clientRequestId"/> was already
    /// applied. Her next visit opens the first-run Welcome, where she records it again. Returns her afterwards, or null
    /// for an unknown teacher.
    /// </summary>
    Task<AdminTeacherRow?> ClearConsentAsync(
        Guid clientRequestId, Guid teacherId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class AdminTeacherStore(AppDbContext db) : IAdminTeacherStore
{
    private const string Endpoint = "admin-clear-consent";

    public Task<IReadOnlyList<AdminTeacherRow>> ListAsync(CancellationToken cancellationToken) =>
        RowsAsync(null, cancellationToken);

    public async Task<AdminTeacherRow?> ClearConsentAsync(
        Guid clientRequestId, Guid teacherId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"teacher-consent:{teacherId}"}, 0))", cancellationToken);
        if (await db.Teachers.SingleOrDefaultAsync(t => t.Id == teacherId, cancellationToken) is not { } teacher)
        {
            return null;
        }

        if (!await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            // Withdrawn consent deletes her greeting recordings (D-44), as re-recording replaces them (GreetingStore).
            await db.AudioClips
                .Where(c => c.TeacherId == teacherId && c.Kind == AudioKind.Greeting)
                .ExecuteDeleteAsync(cancellationToken);
            teacher.ConsentAt = null;
            teacher.GreetingAudioUrl = null;
            db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = Endpoint, CreatedAt = now });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (await RowsAsync(teacherId, cancellationToken)).Single();
    }

    /// <summary>
    /// Every teacher by name (or only <paramref name="teacherId"/>). Her class is the one she is main teacher of, else the
    /// first she teaches in, as on /api/me. Three small queries joined here: there are a handful of teachers.
    /// </summary>
    private async Task<IReadOnlyList<AdminTeacherRow>> RowsAsync(Guid? teacherId, CancellationToken cancellationToken)
    {
        var teachers = await db.Teachers.AsNoTracking()
            .Where(t => teacherId == null || t.Id == teacherId)
            .Select(t => new { t.Id, t.DisplayName, t.ConsentAt })
            .ToListAsync(cancellationToken);
        var ids = teachers.Select(t => t.Id).ToList();
        var classes = (await (
                from m in db.ClassTeachers.AsNoTracking()
                join c in db.Classes on m.ClassId equals c.Id
                join main in db.Teachers on c.TeacherId equals main.Id
                where ids.Contains(m.TeacherId)
                select new { m.TeacherId, IsMain = c.TeacherId == m.TeacherId, MainName = main.DisplayName, c.Grade })
            .ToListAsync(cancellationToken))
            .GroupBy(m => m.TeacherId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.IsMain).First());
        var greetings = (await db.AudioClips.AsNoTracking()
                .Where(c => c.Kind == AudioKind.Greeting && c.TeacherId != null && ids.Contains(c.TeacherId.Value))
                .Select(c => new { TeacherId = c.TeacherId!.Value, c.Seconds })
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.TeacherId)
            .ToDictionary(g => g.Key, g => Math.Round(g.First().Seconds, 1));

        return teachers
            .OrderBy(t => t.DisplayName, StringComparer.Ordinal)
            .Select(t => new AdminTeacherRow(
                t.Id,
                t.DisplayName,
                classes.GetValueOrDefault(t.Id)?.MainName,
                classes.GetValueOrDefault(t.Id)?.Grade,
                t.ConsentAt,
                greetings.TryGetValue(t.Id, out var seconds) ? seconds : null))
            .ToList();
    }
}
