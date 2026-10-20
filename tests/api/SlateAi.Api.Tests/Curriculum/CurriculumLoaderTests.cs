using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

public sealed class CurriculumLoaderTests
{
    [Fact]
    public void Loads_a_valid_file_including_the_textbook_page()
    {
        var graph = CurriculumLoader.Load([("grade3.json", """
            { "grade": 3, "subject": "maths", "version": "2026-10-20",
              "competencies": [
                { "id": "M3.PV.TENS", "nameKn": "ಹತ್ತುಗಳು", "nameEn": "Tens", "chapter": 1, "sequence": 1 },
                { "id": "M3.ADD.CARRY", "nameKn": "ದಶಕ ವರ್ಗಾವಣೆ ಸಂಕಲನ", "nameEn": "Addition with carrying",
                  "prerequisites": ["M3.PV.TENS"], "nipunOutcome": "M-3.4", "chapter": 2, "sequence": 2,
                  "examples": ["27 + 15"], "textbookPage": { "book": "workbook", "part": 1, "page": 34 } }
              ] }
            """)]);

        var carry = Assert.IsType<Competency>(graph.Get("M3.ADD.CARRY"));
        Assert.Equal(["M3.PV.TENS"], carry.Prerequisites);
        Assert.Equal(new TextbookPage("workbook", 1, 34), carry.TextbookPage);
        Assert.Equal(["27 + 15"], carry.Examples);
        Assert.Equal("M-3.4", carry.NipunOutcome);

        var tens = Assert.IsType<Competency>(graph.Get("M3.PV.TENS"));
        Assert.Empty(tens.Prerequisites);
        Assert.Null(tens.TextbookPage);
    }

    [Fact]
    public void Merges_several_files()
    {
        var graph = CurriculumLoader.Load([
            ("a.json", File(Item("M2.PV.ONES"))),
            ("b.json", File(Item("M3.PV.TENS", "M2.PV.ONES"))),
        ]);

        Assert.Equal(["M2.PV.ONES"], graph.Ancestors("M3.PV.TENS"));
    }

    [Theory]
    [InlineData("m3.add", "ID must look like M3.ADD.CARRY")]
    [InlineData("M3", "ID must look like M3.ADD.CARRY")]
    [InlineData("M3.ADD CARRY", "ID must look like M3.ADD.CARRY")]
    public void Rejects_badly_formed_ids(string id, string expected)
    {
        var error = Assert.Throws<CurriculumException>(() => CurriculumLoader.Load([("f.json", File(Item(id)))]));

        Assert.Contains(error.Errors, e => e.Contains(expected));
    }

    [Fact]
    public void Rejects_duplicate_ids_across_files()
    {
        var error = Assert.Throws<CurriculumException>(() => CurriculumLoader.Load([
            ("a.json", File(Item("M3.PV.TENS"))),
            ("b.json", File(Item("M3.PV.TENS"))),
        ]));

        Assert.Equal(["b.json: M3.PV.TENS: duplicate ID (also in a.json)"], error.Errors);
    }

    [Fact]
    public void Rejects_unknown_prerequisites()
    {
        var error = Assert.Throws<CurriculumException>(
            () => CurriculumLoader.Load([("f.json", File(Item("M3.ADD.CARRY", "M3.PV.TENS")))]));

        Assert.Equal(["f.json: M3.ADD.CARRY: unknown prerequisite M3.PV.TENS"], error.Errors);
    }

    [Fact]
    public void Rejects_a_competency_that_is_its_own_prerequisite()
    {
        var error = Assert.Throws<CurriculumException>(
            () => CurriculumLoader.Load([("f.json", File(Item("M3.PV.TENS", "M3.PV.TENS")))]));

        Assert.Equal(["f.json: M3.PV.TENS: lists itself as a prerequisite"], error.Errors);
    }

    [Fact]
    public void Rejects_a_prerequisite_cycle_and_names_the_path()
    {
        var error = Assert.Throws<CurriculumException>(() => CurriculumLoader.Load([("f.json", File(
            Item("M3.A.ONE", "M3.A.THREE"),
            Item("M3.A.TWO", "M3.A.ONE"),
            Item("M3.A.THREE", "M3.A.TWO"),
            Item("M3.B.FREE")))]));

        var cycle = Assert.Single(error.Errors);
        Assert.Equal("prerequisite cycle: M3.A.ONE → M3.A.THREE → M3.A.TWO → M3.A.ONE", cycle);
    }

    [Theory]
    [InlineData("""{ "book": "notebook", "page": 3 }""", "textbookPage.book")]
    [InlineData("""{ "book": "textbook", "page": 0 }""", "must be positive")]
    [InlineData("""{ "book": "textbook", "part": 0, "page": 4 }""", "must be positive")]
    public void Rejects_a_bad_textbook_page(string page, string expected)
    {
        var json = File($$"""{ "id": "M3.PV.TENS", "nameKn": "kn", "nameEn": "en", "textbookPage": {{page}} }""");

        var error = Assert.Throws<CurriculumException>(() => CurriculumLoader.Load([("f.json", json)]));

        Assert.Contains(expected, Assert.Single(error.Errors));
    }

    [Fact]
    public void Reports_every_problem_at_once()
    {
        var error = Assert.Throws<CurriculumException>(() => CurriculumLoader.Load([
            ("a.json", File("""{ "id": "M3.PV.TENS", "nameKn": "", "nameEn": "" }""", Item("M3.ADD.CARRY", "M3.NOPE"))),
            ("b.json", "{ not json"),
        ]));

        Assert.Equal(4, error.Errors.Count);
        Assert.Contains("a.json: M3.PV.TENS: nameEn is required", error.Errors);
        Assert.Contains("a.json: M3.PV.TENS: nameKn is required", error.Errors);
        Assert.Contains("a.json: M3.ADD.CARRY: unknown prerequisite M3.NOPE", error.Errors);
        Assert.Contains(error.Errors, e => e.StartsWith("b.json: not valid JSON"));
    }

    [Fact]
    public void A_missing_directory_gives_an_empty_graph()
    {
        Assert.Empty(CurriculumLoader.LoadDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).All);
    }

    private static string Item(string id, params string[] prerequisites) =>
        $$"""{ "id": "{{id}}", "nameKn": "kn", "nameEn": "en", "prerequisites": [{{string.Join(", ", prerequisites.Select(p => $"\"{p}\""))}}] }""";

    private static string File(params string[] items) =>
        $$"""{ "grade": 3, "subject": "maths", "competencies": [{{string.Join(", ", items)}}] }""";
}
