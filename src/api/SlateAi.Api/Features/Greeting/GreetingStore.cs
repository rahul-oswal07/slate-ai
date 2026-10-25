using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.Greeting;

/// <summary>Database side of the teacher's greeting, kept behind an interface so endpoint tests can use a fake.</summary>
public interface IGreetingStore
{
    /// <summary>
    /// Stores her greeting (replacing the previous one), points <c>Teacher.GreetingAudioUrl</c> at it and records
    /// the voice consent (<c>ConsentAt</c>, only the first time). A replay of the same request changes nothing.
    /// </summary>
    Task SaveAsync(
        Guid teacherId, Guid clientRequestId, byte[] ogg, double seconds, DateTimeOffset at, CancellationToken cancellationToken);
}

public sealed class GreetingStore(AppDbContext db) : IGreetingStore
{
    private const string Endpoint = "greeting";

    public async Task SaveAsync(
        Guid teacherId, Guid clientRequestId, byte[] ogg, double seconds, DateTimeOffset at, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"greeting:{teacherId}"}, 0))", cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return;
        }

        var teacher = await db.Teachers.SingleOrDefaultAsync(t => t.Id == teacherId, cancellationToken);
        if (teacher is null)
        {
            return;
        }

        await db.AudioClips.Where(c => c.TeacherId == teacherId && c.Kind == AudioKind.Greeting).ExecuteDeleteAsync(cancellationToken);
        var clip = new AudioClip
        {
            Id = Guid.NewGuid(),
            Kind = AudioKind.Greeting,
            Language = "kn",
            TeacherId = teacherId,
            ContentType = OpusEncoding.ContentType,
            Bytes = ogg,
            Seconds = seconds,
            CreatedAt = at,
        };
        db.AudioClips.Add(clip);
        teacher.GreetingAudioUrl = AudioSegments.Url(clip.Id);
        teacher.ConsentAt ??= at;
        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = Endpoint, CreatedAt = at });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
