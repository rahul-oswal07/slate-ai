using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

/// <summary>Each class reads its own curriculum (T-145, D-125).</summary>
public sealed class CurriculumProviderTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid Stored = Guid.NewGuid();

    private static readonly CurriculumGraph BuiltIn = new(
        [new Competency("M3.ADD.CARRY", "ಕೂಡು", "Addition with carrying", [], null, 3, 1, [], null)]);

    private readonly FakeCurriculumStore _store = new();
    private readonly CurriculumCache _cache = new();

    [Fact]
    public async Task A_class_without_a_stored_curriculum_follows_the_built_in_one()
    {
        Assert.Same(BuiltIn, await Provider().ForClassAsync(ClassId, Ct));
        Assert.Equal(0, _store.Reads);
    }

    [Fact]
    public async Task A_class_with_a_published_curriculum_follows_it()
    {
        Give(ClassId, Shapes());

        var graph = await Provider().ForClassAsync(ClassId, Ct);

        Assert.Equal(["M3.SHAPE.SIDES", "M3.SHAPE.DIAGONAL"], graph.All.Select(c => c.Id));
        Assert.Equal("Shapes", Assert.Single(graph.Chapters).NameEn);
        Assert.Null(graph.Get("M3.ADD.CARRY"));
    }

    [Fact]
    public async Task A_published_curriculum_is_read_once_and_shared_by_its_classes()
    {
        var second = Guid.NewGuid();
        Give(ClassId, Shapes());
        _store.ClassCurricula[second] = Stored;

        var first = await Provider().ForClassAsync(ClassId, Ct);
        var again = await Provider().ForClassAsync(second, Ct);

        Assert.Same(first, again);
        Assert.Equal(1, _store.Reads);
    }

    [Fact]
    public async Task A_class_pointing_at_a_curriculum_that_is_not_published_fails_loudly()
    {
        _store.ClassCurricula[ClassId] = Stored;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Provider().ForClassAsync(ClassId, Ct));
    }

    [Fact]
    public async Task A_stored_curriculum_gets_every_check_a_file_gets()
    {
        // Changed in the database by hand: a prerequisite cycle the publish step would have refused.
        Give(ClassId, Shapes() with
        {
            Competencies =
            [
                new Competency("M3.SHAPE.SIDES", "ಬದಿ", "Sides", ["M3.SHAPE.DIAGONAL"], null, 1, 1, [], null),
                new Competency("M3.SHAPE.DIAGONAL", "ಕರ್ಣ", "Diagonals", ["M3.SHAPE.SIDES"], null, 1, 2, [], null),
            ],
        });

        var error = await Assert.ThrowsAsync<CurriculumException>(() => Provider().ForClassAsync(ClassId, Ct));

        Assert.Contains(error.Errors, e => e.StartsWith("prerequisite cycle", StringComparison.Ordinal));
    }

    private CurriculumProvider Provider() => new(_store, new BuiltInCurriculum(BuiltIn), _cache);

    private void Give(Guid classId, CurriculumFile curriculum)
    {
        _store.ClassCurricula[classId] = Stored;
        _store.Published[Stored] = curriculum;
    }

    private static CurriculumFile Shapes() => new(
        3, "maths", null,
        [
            new Competency("M3.SHAPE.SIDES", "ಬದಿಗಳು", "Sides and corners", [], null, 1, 1, [], null),
            new Competency("M3.SHAPE.DIAGONAL", "ಕರ್ಣ", "Diagonals", ["M3.SHAPE.SIDES"], null, 1, 2, [], null),
        ],
        [new Chapter(1, "ಆಕೃತಿಗಳು", "Shapes")]);
}
