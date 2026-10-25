using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.Audio;

public static class AudioKind
{
    public const string Script = "script";
    public const string Disclosure = "disclosure";
    public const string Greeting = "greeting";
}

/// <summary>A clip's facts without its bytes.</summary>
/// <param name="ClassId">
/// The class whose catch-up it belongs to; null for shared clips (the disclosure). A greeting belongs to the class its
/// teacher is the main teacher of (<see cref="Guid.Empty"/> if none, so no class can fetch it).
/// </param>
/// <param name="TeacherId">The teacher who recorded it (greetings only); she can always fetch her own.</param>
public sealed record AudioInfo(
    Guid Id, string Kind, string Language, double Seconds, int SizeBytes, Guid? ClassId, Guid? TeacherId = null);

/// <summary>Where audio segments live (Postgres in the MVP), behind an interface so endpoints can be tested with fakes.</summary>
public interface IAudioStore
{
    Task<AudioInfo?> GetInfoAsync(Guid id, CancellationToken cancellationToken);

    Task<byte[]?> GetBytesAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The greeting of the class's main teacher (what families hear first), or null if she has none.</summary>
    Task<AudioInfo?> FindClassGreetingAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>A shared clip made once for this wording (D-18 disclosure), or null.</summary>
    Task<AudioInfo?> FindSharedAsync(string kind, string language, string textHash, CancellationToken cancellationToken);

    /// <summary>Stores a shared clip; if another request stored the same wording first, returns that one.</summary>
    Task<AudioInfo> SaveSharedAsync(string kind, string language, string textHash, SpokenAudio audio, CancellationToken cancellationToken);

    /// <summary>Stores a catch-up's script audio, replacing its previous version.</summary>
    Task<AudioInfo> ReplaceScriptAsync(Guid packageId, string language, SpokenAudio audio, CancellationToken cancellationToken);

    /// <summary>The stored content type (for serving).</summary>
    Task<string?> GetContentTypeAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AudioStore(AppDbContext db, TimeProvider clock) : IAudioStore
{
    public Task<AudioInfo?> GetInfoAsync(Guid id, CancellationToken cancellationToken) =>
        (from c in db.AudioClips
         where c.Id == id
         join p in db.CatchUpPackages on c.PackageId equals p.Id into packages
         from p in packages.DefaultIfEmpty()
         select new AudioInfo(
             c.Id, c.Kind, c.Language, c.Seconds, c.Bytes.Length,
             p != null
                 ? p.ClassId
                 : c.TeacherId == null
                     ? null
                     : db.Classes.Where(k => k.TeacherId == c.TeacherId).Select(k => (Guid?)k.Id).FirstOrDefault() ?? Guid.Empty,
             c.TeacherId))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<AudioInfo?> FindClassGreetingAsync(Guid classId, CancellationToken cancellationToken) =>
        (from k in db.Classes
         where k.Id == classId
         join c in db.AudioClips on k.TeacherId equals c.TeacherId
         where c.Kind == AudioKind.Greeting
         select new AudioInfo(c.Id, c.Kind, c.Language, c.Seconds, c.Bytes.Length, k.Id, c.TeacherId))
        .FirstOrDefaultAsync(cancellationToken);

    public Task<byte[]?> GetBytesAsync(Guid id, CancellationToken cancellationToken) =>
        db.AudioClips.Where(c => c.Id == id).Select(c => c.Bytes).SingleOrDefaultAsync(cancellationToken);

    public Task<AudioInfo?> FindSharedAsync(string kind, string language, string textHash, CancellationToken cancellationToken) =>
        db.AudioClips
            .Where(c => c.Kind == kind && c.Language == language && c.TextHash == textHash)
            .Select(c => new AudioInfo(c.Id, c.Kind, c.Language, c.Seconds, c.Bytes.Length, null))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<string?> GetContentTypeAsync(Guid id, CancellationToken cancellationToken) =>
        db.AudioClips.Where(c => c.Id == id).Select(c => c.ContentType).SingleOrDefaultAsync(cancellationToken);

    public async Task<AudioInfo> SaveSharedAsync(
        string kind, string language, string textHash, SpokenAudio audio, CancellationToken cancellationToken)
    {
        // Two catch-ups can ask for the same disclosure at once; the lock keeps one clip per wording.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var key = $"audio:{kind}:{language}:{textHash}";
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        if (await FindSharedAsync(kind, language, textHash, cancellationToken) is { } existing)
        {
            return existing;
        }

        var clip = New(kind, language, audio);
        clip.TextHash = textHash;
        db.AudioClips.Add(clip);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AudioInfo(clip.Id, kind, language, audio.Seconds, audio.Audio.Length, null);
    }

    public async Task<AudioInfo> ReplaceScriptAsync(
        Guid packageId, string language, SpokenAudio audio, CancellationToken cancellationToken)
    {
        await db.AudioClips.Where(c => c.PackageId == packageId && c.Kind == AudioKind.Script).ExecuteDeleteAsync(cancellationToken);
        var clip = New(AudioKind.Script, language, audio);
        clip.PackageId = packageId;
        db.AudioClips.Add(clip);
        await db.SaveChangesAsync(cancellationToken);
        var classId = await db.CatchUpPackages.Where(p => p.Id == packageId).Select(p => p.ClassId).SingleAsync(cancellationToken);
        return new AudioInfo(clip.Id, AudioKind.Script, language, audio.Seconds, audio.Audio.Length, classId);
    }

    private AudioClip New(string kind, string language, SpokenAudio audio) => new()
    {
        Id = Guid.NewGuid(),
        Kind = kind,
        Language = language,
        ContentType = audio.ContentType,
        Bytes = audio.Audio,
        Seconds = audio.Seconds,
        CreatedAt = clock.GetUtcNow(),
    };
}
