using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

/// <summary>In-memory <see cref="ICurriculumStore"/>: every class follows the built-in curriculum unless a test says so.</summary>
public sealed class FakeCurriculumStore : ICurriculumStore
{
    private readonly Lock _lock = new();

    /// <summary>Class → the stored curriculum it follows.</summary>
    public Dictionary<Guid, Guid> ClassCurricula { get; } = [];

    /// <summary>Published curricula by ID.</summary>
    public Dictionary<Guid, CurriculumFile> Published { get; } = [];

    /// <summary>How many times a published curriculum was read, to prove the graph is cached.</summary>
    public int Reads { get; private set; }

    public Task<Guid?> GetClassCurriculumIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(ClassCurricula.TryGetValue(classId, out var id) ? id : (Guid?)null);
        }
    }

    public Task<CurriculumFile?> GetPublishedAsync(Guid curriculumId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            Reads++;
            return Task.FromResult(Published.GetValueOrDefault(curriculumId));
        }
    }
}
