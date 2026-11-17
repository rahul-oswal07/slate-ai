using System.Collections.Concurrent;

namespace SlateAi.Api.Curriculum;

/// <summary>
/// The curriculum a class follows (T-145, D-125). Everything that reads lessons, chapters or prerequisites for a class goes
/// through this, never a single app-wide graph, because classes can follow different books.
/// </summary>
public interface ICurriculumProvider
{
    /// <summary>The class's published curriculum, or the built-in one when it has none (or there is no such class).</summary>
    Task<CurriculumGraph> ForClassAsync(Guid classId, CancellationToken cancellationToken);
}

/// <summary>
/// The curriculum from <c>data/curriculum/*.json</c>, checked at startup (T-05). Classes without a stored curriculum use
/// it; the admin lesson-link and lesson-content pages and the lesson-content file describe it.
/// </summary>
public sealed record BuiltInCurriculum(CurriculumGraph Graph);

/// <summary>Graphs of stored curricula by ID. Safe to keep for the app's life: a published curriculum never changes.</summary>
public sealed class CurriculumCache
{
    private readonly ConcurrentDictionary<Guid, CurriculumGraph> _graphs = new();

    public bool TryGet(Guid curriculumId, out CurriculumGraph graph) => _graphs.TryGetValue(curriculumId, out graph!);

    public void Add(Guid curriculumId, CurriculumGraph graph) => _graphs.TryAdd(curriculumId, graph);
}

public sealed class CurriculumProvider(ICurriculumStore store, BuiltInCurriculum builtIn, CurriculumCache cache)
    : ICurriculumProvider
{
    public async Task<CurriculumGraph> ForClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        if (await store.GetClassCurriculumIdAsync(classId, cancellationToken) is not { } curriculumId)
        {
            return builtIn.Graph;
        }

        if (cache.TryGet(curriculumId, out var cached))
        {
            return cached;
        }

        // Publishing checks the whole curriculum and only a published one can be given to a class, so a failure here
        // means the database was changed by hand: fail loudly rather than teach from the wrong book.
        var file = await store.GetPublishedAsync(curriculumId, cancellationToken)
                   ?? throw new InvalidOperationException($"Class {classId} follows curriculum {curriculumId}, which is not published.");
        var graph = CurriculumLoader.Load([($"curriculum {curriculumId}", file)]);
        cache.Add(curriculumId, graph);
        return graph;
    }
}
