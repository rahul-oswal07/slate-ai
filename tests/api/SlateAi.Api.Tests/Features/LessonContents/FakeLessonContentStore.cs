using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Tests.Features.LessonContents;

/// <summary>In-memory lesson descriptions (T-141) with the real store's replay and keep-the-edit rules.</summary>
public sealed class FakeLessonContentStore : ILessonContentStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _processed = [];
    private readonly HashSet<string> _edited = [];

    public Dictionary<string, StoredLessonContent> Contents { get; } = [];

    /// <summary>How many requests changed something (replays don't count).</summary>
    public int Writes { get; private set; }

    public Task<IReadOnlyDictionary<string, StoredLessonContent>> GetAllAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyDictionary<string, StoredLessonContent>>(new Dictionary<string, StoredLessonContent>(Contents));
        }
    }

    public Task<IReadOnlyDictionary<string, StoredLessonContent>> GetForAsync(
        IReadOnlyList<string> competencyIds, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyDictionary<string, StoredLessonContent>>(
                competencyIds.Where(Contents.ContainsKey).Distinct().ToDictionary(id => id, id => Contents[id]));
        }
    }

    public Task<StoredLessonContent?> SetAsync(
        Guid clientRequestId, string competencyId, LessonContentText? content, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(Contents.GetValueOrDefault(competencyId));
            }

            Writes++;
            if (content is null)
            {
                Contents.Remove(competencyId);
                _edited.Remove(competencyId);
                return Task.FromResult<StoredLessonContent?>(null);
            }

            var stored = new StoredLessonContent(content.Description, content.Examples, LessonContentSources.Edited);
            Contents[competencyId] = stored;
            _edited.Add(competencyId);
            return Task.FromResult<StoredLessonContent?>(stored);
        }
    }

    public Task<LessonContentSyncResult> SyncAsync(
        IReadOnlyList<LessonContentEntry> entries, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            int added = 0, updated = 0, kept = 0;
            foreach (var entry in entries)
            {
                var stored = new StoredLessonContent(entry.Content.Description, entry.Content.Examples, entry.Source);
                if (!Contents.TryGetValue(entry.CompetencyId, out var existing))
                {
                    added++;
                }
                else if (_edited.Contains(entry.CompetencyId))
                {
                    kept++;
                    continue;
                }
                else if (existing.Description == stored.Description && existing.Examples.SequenceEqual(stored.Examples)
                    && existing.Source == stored.Source)
                {
                    continue;
                }
                else
                {
                    updated++;
                }

                Contents[entry.CompetencyId] = stored;
            }

            return Task.FromResult(new LessonContentSyncResult(added, updated, kept));
        }
    }
}
