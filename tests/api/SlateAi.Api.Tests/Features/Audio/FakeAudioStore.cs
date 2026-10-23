using SlateAi.Api.Features.Audio;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Tests.Features.Audio;

/// <summary>In-memory <see cref="IAudioStore"/>.</summary>
public sealed class FakeAudioStore : IAudioStore
{
    private readonly Lock _gate = new();

    public List<(AudioInfo Info, byte[] Bytes, string? Hash, Guid? PackageId, string ContentType)> Clips { get; } = [];

    /// <summary>The class each package belongs to, for <see cref="AudioInfo.ClassId"/>.</summary>
    public Dictionary<Guid, Guid> PackageClasses { get; } = [];

    public Task<AudioInfo?> GetInfoAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Clips.Where(c => c.Info.Id == id).Select(c => c.Info).Cast<AudioInfo?>().FirstOrDefault());
        }
    }

    public Task<byte[]?> GetBytesAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Clips.Where(c => c.Info.Id == id).Select(c => c.Bytes).Cast<byte[]?>().FirstOrDefault());
        }
    }

    public Task<AudioInfo?> FindSharedAsync(string kind, string language, string textHash, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Clips.Where(c => c.Info.Kind == kind && c.Info.Language == language && c.Hash == textHash)
                .Select(c => c.Info).Cast<AudioInfo?>().FirstOrDefault());
        }
    }

    public Task<string?> GetContentTypeAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Clips.Where(c => c.Info.Id == id).Select(c => c.ContentType).Cast<string?>().FirstOrDefault());
        }
    }

    public Task<AudioInfo> SaveSharedAsync(string kind, string language, string textHash, SpokenAudio audio, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var info = new AudioInfo(Guid.NewGuid(), kind, language, audio.Seconds, audio.Audio.Length, null);
            Clips.Add((info, audio.Audio, textHash, null, audio.ContentType));
            return Task.FromResult(info);
        }
    }

    public Task<AudioInfo> ReplaceScriptAsync(Guid packageId, string language, SpokenAudio audio, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Clips.RemoveAll(c => c.PackageId == packageId && c.Info.Kind == AudioKind.Script);
            var info = new AudioInfo(Guid.NewGuid(), AudioKind.Script, language, audio.Seconds, audio.Audio.Length, PackageClasses.GetValueOrDefault(packageId));
            Clips.Add((info, audio.Audio, null, packageId, audio.ContentType));
            return Task.FromResult(info);
        }
    }
}
