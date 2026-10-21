using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Curriculum;

public sealed class CurriculumStartupTests
{
    [Fact]
    public void Every_shipped_curriculum_file_is_valid()
    {
        // The API's build output carries data/curriculum/*.json. Guards the competency list (T-22) once it lands:
        // known prerequisites, no cycles, well-formed IDs and textbook pages.
        var directory = Path.Combine(AppContext.BaseDirectory, "data", "curriculum");

        var graph = CurriculumLoader.LoadDirectory(directory);

        if (Directory.Exists(directory) && Directory.EnumerateFiles(directory, "*.json").Any())
        {
            Assert.NotEmpty(graph.All);
        }
    }

    [Fact]
    public void The_grade3_curriculum_gives_every_competency_a_homework_page()
    {
        // D-22: homework points to the child's own book, so a missing page is a content bug.
        var graph = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));

        Assert.Equal(16, graph.All.Count);
        Assert.All(graph.All, c => Assert.NotNull(c.TextbookPage));
    }

    [Fact]
    public void The_grade3_curriculum_keeps_only_what_next_week_builds_on()
    {
        var graph = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));

        // Away for no-carry addition, face value and comparing; next week is carrying.
        var candidates = graph.CandidateSet(
            missed: ["M3.ADD.NOCARRY", "M3.PV.FACE", "M3.NUM.COMPARE"],
            nextWeek: ["M3.ADD.CARRY"]);

        Assert.Equal(["M3.ADD.NOCARRY"], candidates);
    }

    [Fact]
    public async Task The_api_starts_with_an_empty_curriculum()
    {
        using var directory = new TempDirectory();
        await using var factory = FakeApi.Create(settings: Settings(directory.Path));

        Assert.Empty(factory.Services.GetRequiredService<CurriculumGraph>().All);
    }

    [Fact]
    public async Task An_invalid_curriculum_file_stops_startup()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "bad.json"),
            """{ "grade": 3, "subject": "maths", "competencies": [{ "id": "bad", "nameKn": "kn", "nameEn": "en" }] }""",
            TestContext.Current.CancellationToken);
        await using var factory = FakeApi.Create(settings: Settings(directory.Path));

        var error = Assert.Throws<CurriculumException>(() => factory.Services);

        Assert.Contains("bad.json: bad: ID must look like M3.ADD.CARRY", error.Errors);
    }

    private static Dictionary<string, string?> Settings(string directory) =>
        new() { ["Curriculum:Directory"] = directory };

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("curriculum-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
