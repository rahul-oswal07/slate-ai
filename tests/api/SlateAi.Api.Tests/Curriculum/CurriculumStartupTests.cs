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

        // The 2026-27 textbook, Parts 1 and 2 (T-81), plus the Nalikali demo lesson (T-87).
        Assert.Equal(61, graph.All.Count);
        Assert.All(graph.All, c => Assert.NotNull(c.TextbookPage));
    }

    [Fact]
    public void The_nalikali_demo_lesson_is_the_bridge_course_s_page_4_and_only_it_names_nalikali()
    {
        // T-87, D-90: the other 60 lessons keep the D-89 pages until they are re-pointed after the final.
        var graph = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));

        var lesson = Assert.IsType<Competency>(graph.Get("M3.BRIDGE.COMPARE"));
        Assert.Equal(0, lesson.Chapter);
        Assert.Equal(new TextbookPage(TextbookPage.Nalikali, 1, 4), lesson.TextbookPage);
        Assert.Contains("ಹೆಚ್ಚು", lesson.KeyWordsKn!);
        Assert.Null(lesson.Practice);
        Assert.Equal(10, lesson.Exercises!.Compare!.Count);
        Assert.Equal(5, lesson.Exercises.Extremes!.Count);
        Assert.Equal(["M3.BRIDGE.COMPARE"], graph.All.Where(c => c.TextbookPage?.Book == TextbookPage.Nalikali).Select(c => c.Id));
        Assert.Contains(graph.Chapters, c => c.Number == 0);
        Assert.NotEqual("M3.BRIDGE.COMPARE", graph.NextInBook(null)?.Id);
    }

    [Fact]
    public void The_grade3_curriculum_names_every_lesson_for_every_family_language()
    {
        // FR-20: a family whose language has no lesson name gets no evening message that day.
        var graph = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));

        Assert.All(graph.All, c => Assert.All(["kn", "en", "te", "ta", "hi"], language => Assert.NotNull(c.Name(language))));
    }

    [Fact]
    public void The_grade3_curriculum_keeps_only_what_next_week_builds_on()
    {
        var graph = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));

        // Away for no-carry addition, place value and comparing; next week is carrying.
        var candidates = graph.CandidateSet(
            missed: ["M3.ADD.NOCARRY", "M3.PV.EXPAND", "M3.NUM.COMPARE"],
            nextWeek: ["M3.ADD.CARRY"]);

        Assert.Equal(["M3.ADD.NOCARRY"], candidates);
    }

    [Fact]
    public void The_grade3_curriculum_has_no_catch_up_for_a_lesson_without_practice()
    {
        var graph = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));

        // Tables and place value both lead to 2-digit × 1-digit, but code can't make sums for tables yet.
        var candidates = graph.CandidateSet(missed: ["M3.MUL.TABLES", "M3.PV.HTO"], nextWeek: ["M3.MUL.2D1D"]);

        Assert.Equal(["M3.PV.HTO"], candidates);
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
