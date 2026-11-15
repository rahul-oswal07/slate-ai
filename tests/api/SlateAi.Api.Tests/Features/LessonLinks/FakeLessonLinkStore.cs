using SlateAi.Api.Features.LessonLinks;

namespace SlateAi.Api.Tests.Features.LessonLinks;

/// <summary>In-memory lesson links (T-94) with the real store's replay rule.</summary>
public sealed class FakeLessonLinkStore : ILessonLinkStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _processed = [];

    public Dictionary<string, string> Links { get; } = [];

    /// <summary>How many requests changed something (replays don't count).</summary>
    public int Writes { get; private set; }

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(Links));
        }
    }

    public Task<IReadOnlyList<(string CompetencyId, string Url)>> GetForAsync(
        IReadOnlyList<string> competencyIds, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<(string, string)>>(
                [.. competencyIds.Where(Links.ContainsKey).Select(id => (id, Links[id]))]);
        }
    }

    public Task<string?> SetAsync(
        Guid clientRequestId, string competencyId, string? url, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(Links.GetValueOrDefault(competencyId));
            }

            Writes++;
            if (url is null)
            {
                Links.Remove(competencyId);
            }
            else
            {
                Links[competencyId] = url;
            }

            return Task.FromResult(url);
        }
    }
}
