using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Features.Audio;

namespace SlateAi.Api.Features.Admin;

/// <summary>One teacher's recorded greeting, without its bytes.</summary>
public sealed record AdminGreeting(Guid ClipId, string TeacherName, double Seconds, DateTimeOffset RecordedAt);

/// <summary>A greeting's audio.</summary>
public sealed record AdminClip(byte[] Bytes, string ContentType);

/// <summary>Database side of the admin page, kept behind an interface so endpoint tests can use a fake.</summary>
public interface IAdminStore
{
    /// <summary>Every teacher's greeting, newest first.</summary>
    Task<IReadOnlyList<AdminGreeting>> ListGreetingsAsync(CancellationToken cancellationToken);

    /// <summary>The clip's audio if it is a greeting; null for any other clip.</summary>
    Task<AdminClip?> GetGreetingAsync(Guid clipId, CancellationToken cancellationToken);
}

public sealed class AdminStore(AppDbContext db) : IAdminStore
{
    public async Task<IReadOnlyList<AdminGreeting>> ListGreetingsAsync(CancellationToken cancellationToken)
    {
        var rows = await (
                from clip in db.AudioClips
                where clip.Kind == AudioKind.Greeting
                join teacher in db.Teachers on clip.TeacherId equals teacher.Id
                select new { clip.Id, teacher.DisplayName, clip.Seconds, clip.CreatedAt })
            .ToListAsync(cancellationToken);
        return rows
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminGreeting(r.Id, r.DisplayName, Math.Round(r.Seconds, 1), r.CreatedAt))
            .ToList();
    }

    public async Task<AdminClip?> GetGreetingAsync(Guid clipId, CancellationToken cancellationToken) =>
        await db.AudioClips
            .Where(c => c.Id == clipId && c.Kind == AudioKind.Greeting)
            .Select(c => new AdminClip(c.Bytes, c.ContentType))
            .SingleOrDefaultAsync(cancellationToken);
}
