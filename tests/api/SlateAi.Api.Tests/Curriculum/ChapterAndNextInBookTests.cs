using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

public sealed class ChapterAndNextInBookTests
{
    private static readonly CurriculumGraph Book = new(
    [
        Item("M2.ADD.2D", chapter: 0, sequence: 1),
        Item("M3.PV.HTO", chapter: 2, sequence: 2),
        Item("M3.ADD.CARRY", chapter: 4, sequence: 3),
        Item("M3.SUB.BORROW", chapter: 5, sequence: 4),
    ]);

    [Fact]
    public void With_nothing_taught_it_suggests_the_first_lesson_after_the_bridge_course() =>
        Assert.Equal("M3.PV.HTO", Book.NextInBook(null)?.Id);

    [Fact]
    public void Suggests_the_lesson_after_the_last_one_taught_even_across_chapters() =>
        Assert.Equal("M3.SUB.BORROW", Book.NextInBook("M3.ADD.CARRY")?.Id);

    [Fact]
    public void Nothing_after_the_last_lesson_in_the_book() => Assert.Null(Book.NextInBook("M3.SUB.BORROW"));

    [Fact]
    public void An_unknown_last_lesson_starts_after_the_bridge_course() =>
        Assert.Equal("M3.PV.HTO", Book.NextInBook("M9.NOPE")?.Id);

    [Fact]
    public void Loads_chapter_titles()
    {
        var graph = CurriculumLoader.Load([("g3.json", """
            { "grade": 3, "subject": "maths",
              "chapters": [ { "number": 5, "nameKn": "ಕಳೆಯುವ ಮೋಜು", "nameEn": "The fun of subtracting" } ],
              "competencies": [ { "id": "M3.SUB.BORROW", "nameKn": "ಕ", "nameEn": "Borrowing", "chapter": 5, "sequence": 1 } ] }
            """)]);

        Assert.Equal(new Chapter(5, "ಕಳೆಯುವ ಮೋಜು", "The fun of subtracting"), Assert.Single(graph.Chapters));
    }

    [Fact]
    public void A_competency_in_a_chapter_the_file_does_not_list_is_an_error()
    {
        var error = Assert.Throws<CurriculumException>(() => CurriculumLoader.Load([("g3.json", """
            { "grade": 3, "subject": "maths",
              "chapters": [ { "number": 5, "nameKn": "ಕ", "nameEn": "Five" } ],
              "competencies": [ { "id": "M3.ADD.CARRY", "nameKn": "ಕ", "nameEn": "Carry", "chapter": 4, "sequence": 1 } ] }
            """)]));

        Assert.Contains(error.Errors, e => e.Contains("chapter 4 is not in this file's chapters"));
    }

    [Fact]
    public void The_shipped_curriculum_has_a_title_for_every_chapter()
    {
        var graph = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));
        var titled = graph.Chapters.Select(c => c.Number).ToHashSet();

        Assert.NotEmpty(graph.All);
        Assert.All(graph.All, c => Assert.Contains(c.Chapter, titled));
    }

    private static Competency Item(string id, int chapter, int sequence) =>
        new(id, "ಕ", id, [], null, chapter, sequence, [], null);
}
