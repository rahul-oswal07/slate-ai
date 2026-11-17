using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Tests.Features.LessonContents;

/// <summary>
/// In-memory lesson descriptions (T-141) with the real store's replay and keep-the-edit rules, per curriculum (T-147).
/// <see cref="Contents"/> is the built-in curriculum's.
/// </summary>
public sealed class FakeLessonContentStore : ILessonContentStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _processed = [];
    private readonly HashSet<(Guid, string)> _edited = [];

    /// <summary>Curriculum ID → competency ID → content.</summary>
    public Dictionary<Guid, Dictionary<string, StoredLessonContent>> ByCurriculum { get; } = new() { [BuiltInCurriculum.Id] = [] };

    /// <summary>The built-in curriculum's descriptions.</summary>
    public Dictionary<string, StoredLessonContent> Contents => ByCurriculum[BuiltInCurriculum.Id];

    private Dictionary<string, StoredLessonContent> Of(Guid curriculumId) =>
        ByCurriculum.TryGetValue(curriculumId, out var contents) ? contents : ByCurriculum[curriculumId] = [];

    /// <summary>How many requests changed something (replays don't count).</summary>
    public int Writes { get; private set; }

    public Task<IReadOnlyDictionary<string, StoredLessonContent>> GetAllAsync(Guid curriculumId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyDictionary<string, StoredLessonContent>>(new Dictionary<string, StoredLessonContent>(Of(curriculumId)));
        }
    }

    public Task<IReadOnlyDictionary<string, StoredLessonContent>> GetForAsync(
        Guid curriculumId, IReadOnlyList<string> competencyIds, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var contents = Of(curriculumId);
            return Task.FromResult<IReadOnlyDictionary<string, StoredLessonContent>>(
                competencyIds.Where(contents.ContainsKey).Distinct().ToDictionary(id => id, id => contents[id]));
        }
    }

    public Task<StoredLessonContent?> SetAsync(
        Guid clientRequestId, Guid curriculumId, string competencyId, LessonContentText? content, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var contents = Of(curriculumId);
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(contents.GetValueOrDefault(competencyId));
            }

            Writes++;
            if (content is null)
            {
                contents.Remove(competencyId);
                _edited.Remove((curriculumId, competencyId));
                return Task.FromResult<StoredLessonContent?>(null);
            }

            var stored = new StoredLessonContent(content.Description, content.Examples, LessonContentSources.Edited);
            contents[competencyId] = stored;
            _edited.Add((curriculumId, competencyId));
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
                else if (_edited.Contains((BuiltInCurriculum.Id, entry.CompetencyId)))
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
