using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

/// <summary>Every class follows <paramref name="graph"/>: for services a unit test builds by hand (T-145).</summary>
/// <param name="id">The curriculum's ID, which keys its descriptions and links (T-147); the built-in one by default.</param>
public sealed class FixedCurriculumProvider(CurriculumGraph graph, Guid? id = null) : ICurriculumProvider
{
    public Task<CurriculumGraph> ForClassAsync(Guid classId, CancellationToken cancellationToken) => Task.FromResult(graph);

    public Task<Guid> IdForClassAsync(Guid classId, CancellationToken cancellationToken) => Task.FromResult(id ?? BuiltInCurriculum.Id);
}
