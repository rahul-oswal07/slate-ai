using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

/// <summary>Every class follows <paramref name="graph"/>: for services a unit test builds by hand (T-145).</summary>
public sealed class FixedCurriculumProvider(CurriculumGraph graph) : ICurriculumProvider
{
    public Task<CurriculumGraph> ForClassAsync(Guid classId, CancellationToken cancellationToken) => Task.FromResult(graph);
}
