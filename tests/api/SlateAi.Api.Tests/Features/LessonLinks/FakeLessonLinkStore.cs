using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonLinks;

namespace SlateAi.Api.Tests.Features.LessonLinks;

/// <summary>In-memory lesson links (T-94) with the real store's replay rule, per curriculum (T-147); <see cref="Links"/> is the built-in one's.</summary>
public sealed class FakeLessonLinkStore : ILessonLinkStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _processed = [];

    /// <summary>Curriculum ID → competency ID → URL.</summary>
    public Dictionary<Guid, Dictionary<string, string>> ByCurriculum { get; } = new() { [BuiltInCurriculum.Id] = [] };

    /// <summary>The built-in curriculum's links.</summary>
    public Dictionary<string, string> Links => ByCurriculum[BuiltInCurriculum.Id];

    private Dictionary<string, string> Of(Guid curriculumId) =>
        ByCurriculum.TryGetValue(curriculumId, out var links) ? links : ByCurriculum[curriculumId] = [];

    /// <summary>How many requests changed something (replays don't count).</summary>
    public int Writes { get; private set; }

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(Guid curriculumId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(Of(curriculumId)));
        }
    }

    public Task<IReadOnlyList<(string CompetencyId, string Url)>> GetForAsync(
        Guid curriculumId, IReadOnlyList<string> competencyIds, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var links = Of(curriculumId);
            return Task.FromResult<IReadOnlyList<(string, string)>>(
                [.. competencyIds.Where(links.ContainsKey).Select(id => (id, links[id]))]);
        }
    }

    public Task<string?> SetAsync(
        Guid clientRequestId, Guid curriculumId, string competencyId, string? url, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var links = Of(curriculumId);
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(links.GetValueOrDefault(competencyId));
            }

            Writes++;
            if (url is null)
            {
                links.Remove(competencyId);
            }
            else
            {
                links[competencyId] = url;
            }

            return Task.FromResult(url);
        }
    }
}
