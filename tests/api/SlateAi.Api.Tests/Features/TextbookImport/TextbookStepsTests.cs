using System.Text.Json;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Tests.Features.TextbookImport;

/// <summary>T-146: every field A10 returns is checked by code; what fails is dropped and flagged, never passed through.</summary>
public sealed class TextbookStepsTests
{
    internal static readonly ImportBook Book = new("Grade 3 maths", 3, "maths", "kn", "textbook", 1);

    // --- Stage 1: outline -------------------------------------------------------------------------------------------

    [Fact]
    public void Outline_pages_are_numbered_in_the_book_and_a_missing_or_invalid_page_is_kept_as_other()
    {
        var json = Json(new
        {
            pages = new object[]
            {
                OutlinePage(1, 12, "lesson", starts: true, number: 2, kn: "ಕೂಡುವುದು", en: "Addition"),
                OutlinePage(1, 99, "exercises"), // twice: the first one counts
                OutlinePage(3, 5000, "weird"),   // printed page out of range, unknown kind
                OutlinePage(7, 1, "lesson"),     // not in the window
            },
        });

        var pages = TextbookOutline.Validate(json, firstPage: 21, count: 3)!;

        Assert.Equal([21, 22, 23], pages.Select(p => p.Number));
        Assert.Equal(new ChapterStart(2, "ಕೂಡುವುದು", "Addition"), pages[0].ChapterStart);
        Assert.Equal(12, pages[0].PrintedPage);
        Assert.Equal((PageKinds.Other, (int?)null), (pages[1].Kind, pages[1].PrintedPage));
        Assert.Equal((PageKinds.Other, (int?)null), (pages[2].Kind, pages[2].PrintedPage));
    }

    [Fact]
    public void Outline_output_that_is_not_json_is_refused()
    {
        Assert.Null(TextbookOutline.Validate("not json", 1, 3));
        Assert.Null(TextbookOutline.Validate("{}", 1, 3));
    }

    [Fact]
    public void Chapters_run_from_their_first_page_to_the_next_and_leave_out_contents_and_answers()
    {
        OutlinePage[] book =
        [
            new(1, null, PageKinds.Front, null),
            new(2, null, PageKinds.Contents, new ChapterStart(1, "ಸಂಖ್ಯೆಗಳು", "Numbers")), // the contents lists it: not a start
            new(3, 1, PageKinds.Lesson, new ChapterStart(1, "ಸಂಖ್ಯೆಗಳು", "Numbers")),
            new(4, 2, PageKinds.Exercises, null),
            new(5, 3, PageKinds.Lesson, new ChapterStart(2, "ಕೂಡುವುದು", "Addition")),
            new(6, 4, PageKinds.Answers, null),
        ];

        var outline = TextbookOutline.Assemble(book)!;

        Assert.Equal(
            [(1, "Numbers", "3,4"), (2, "Addition", "5")],
            outline.Chapters.Select(c => (c.Number, c.NameEn, string.Join(",", c.Pages))));
        Assert.Equal(3, outline.PrintedPages[5]);
        Assert.Empty(outline.Flags);
    }

    [Fact]
    public void Chapter_numbers_out_of_order_are_numbered_in_order_and_flagged_and_a_book_without_chapters_has_no_outline()
    {
        var outline = TextbookOutline.Assemble(
        [
            new(1, 1, PageKinds.Lesson, new ChapterStart(5, "ಎ", "A")),
            new(2, 2, PageKinds.Lesson, new ChapterStart(3, "", "")),
        ])!;

        Assert.Equal([5, 6], outline.Chapters.Select(c => c.Number));
        Assert.Equal("Chapter 6", outline.Chapters[1].NameEn);
        Assert.Equal("Chapter 6", outline.Chapters[1].NameKn);
        Assert.Equal(2, outline.Flags.Count);
        Assert.Null(TextbookOutline.Assemble([new(1, 1, PageKinds.Lesson, null)]));
    }

    // --- Stage 2: lessons -------------------------------------------------------------------------------------------

    [Fact]
    public void Exercises_that_break_a_rule_are_left_out_and_flagged_and_the_rest_are_kept_once()
    {
        var json = Json(new { lessons = new[] { Lesson(exercises: new
        {
            compare = new[] { new[] { 48, 33 }, new[] { 48, 33 }, new[] { 1, 2, 3 } },
            extremes = new[] { new[] { 3, 38, 64 } },
            add = new[] { new[] { 248, 438 }, new[] { 600, 500 } },
            sub = new[] { new[] { 872, 586 }, new[] { 3, 5 } },
        }) } });

        var lesson = Assert.Single(TextbookLessons.Validate(json, [10, 11], new Dictionary<int, int>())!);

        Assert.Equal([[48, 33]], lesson.Exercises!.Compare!.Select(p => p.ToArray()));
        Assert.Equal([[248, 438]], lesson.Exercises.Add!.Select(p => p.ToArray()));
        Assert.Equal([[872, 586]], lesson.Exercises.Sub!.Select(p => p.ToArray()));
        Assert.Equal(3, lesson.Flags.Count(f => f.StartsWith("Exercise left out", StringComparison.Ordinal)));
        Assert.Contains(lesson.Flags, f => f.Contains("add 600, 500"));
    }

    [Fact]
    public void Pages_are_the_part_s_pdf_pages_and_notes_on_a_page_outside_the_part_go_on_the_lesson_s_first_page()
    {
        var json = Json(new { lessons = new[] { Lesson(pages: [2, 9], otherPage: 9, picturePage: 1) } });

        var lesson = Assert.Single(TextbookLessons.Validate(json, [30, 31, 33], new Dictionary<int, int> { [30] = 18, [31] = 19 })!);

        Assert.Equal([31], lesson.Pages);
        Assert.Equal(new OtherExercise(31, 19, "Fill in the missing numbers"), Assert.Single(lesson.OtherExercises));
        var picture = Assert.Single(lesson.Pictures);
        Assert.Equal((30, (int?)18, "top"), (picture.Page, picture.PrintedPage, picture.Position));
    }

    [Fact]
    public void A_description_that_breaks_the_rules_is_left_out_and_flagged_and_a_lesson_without_a_name_is_dropped()
    {
        var json = Json(new
        {
            lessons = new[]
            {
                Lesson(description: new string('x', 701)),
                Lesson(nameKn: "", nameEn: "  "),
                Lesson(key: "m3.add carry", nameKn: "", nameEn: "Addition"),
            },
        });

        var lessons = TextbookLessons.Validate(json, [1], new Dictionary<int, int>())!;

        Assert.Equal(2, lessons.Count);
        Assert.Null(lessons[0].Content);
        Assert.Contains(lessons[0].Flags, f => f.StartsWith("The description was left out", StringComparison.Ordinal));
        Assert.Equal(("ADDCARRY", "Addition"), (lessons[1].Key, lessons[1].NameKn));
        Assert.Contains(lessons[1].Flags, f => f.StartsWith("No Kannada name", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ADD.CARRY", "x", "ADD.CARRY")]
    [InlineData("M3.Add.Carry", "x", "ADD.CARRY")]
    [InlineData("add-with carry", "x", "ADDWITHCARRY")]
    [InlineData("..", "Sides and corners", "SIDES.AND.CORNERS")]
    [InlineData("ಕೂಡು", "", "LESSON")]
    [InlineData("A.B.C.D.E", "x", "A.B.C.D")]
    public void Keys_become_upper_case_letters_and_digits_in_dot_separated_parts(string key, string nameEn, string expected) =>
        Assert.Equal(expected, TextbookLessons.Key(key, nameEn));

    [Fact]
    public void A_lesson_that_carries_on_into_the_next_part_is_one_lesson_with_both_parts_pages_and_exercises()
    {
        var first = Extracted("ADD.CARRY", [10, 11], new ExerciseList(Add: [[248, 438]]));
        var second = Extracted("ADD.CARRY", [12], new ExerciseList(Add: [[248, 438], [125, 25]])) with { NameEn = "Other name" };
        var next = Extracted("ADD.WORD", [13], null);

        var merged = TextbookLessons.Merge([[first], [second, next]]);

        Assert.Equal(["ADD.CARRY", "ADD.WORD"], merged.Select(l => l.Key));
        Assert.Equal("Addition", merged[0].NameEn);
        Assert.Equal([10, 11, 12], merged[0].Pages);
        Assert.Equal([[248, 438], [125, 25]], merged[0].Exercises!.Add!.Select(p => p.ToArray()));
    }

    // --- Stage 3: prerequisites -------------------------------------------------------------------------------------

    [Fact]
    public void Prerequisites_are_earlier_lessons_only_known_ones_and_at_most_four()
    {
        var sequence = new Dictionary<string, int> { ["A"] = 1, ["B"] = 2, ["C"] = 3, ["D"] = 4, ["E"] = 5, ["F"] = 6, ["G"] = 7 };
        var json = Json(new
        {
            lessons = new object[]
            {
                new { id = "B", prerequisites = new[] { "A", "C", "B", "X" } }, // C comes later, B is itself, X unknown
                new { id = "G", prerequisites = new[] { "A", "B", "C", "D", "E", "A" } },
                new { id = "Q", prerequisites = new[] { "A" } },
            },
        });

        var result = TextbookPrerequisites.Validate(json, sequence)!;

        Assert.Equal(["A"], result["B"]);
        Assert.Equal(["A", "B", "C", "D"], result["G"]);
        Assert.False(result.ContainsKey("Q"));
        Assert.Null(TextbookPrerequisites.Validate("[]", sequence));
    }

    // --- Assembly ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Ids_are_made_unique_pages_come_from_the_printed_numbers_and_a_bad_practice_rule_is_left_out()
    {
        var outline = new BookOutline(
            [new OutlineChapter(1, "ಎ", "One", [10, 11]), new OutlineChapter(2, "ಬಿ", "Two", [12])],
            new Dictionary<int, int> { [11] = 7, [12] = 8 },
            []);
        var lessons = DraftAssembler.Lessons(Book, outline,
        [
            (outline.Chapters[0], [Extracted("ADD.CARRY", [10, 11], null) with { Practice = new PracticeRule("add", 5, "any") }]),
            (outline.Chapters[1], [Extracted("ADD.CARRY", [12], null), Extracted("SUB", [99], null)]),
        ]);

        Assert.Equal(["M3.ADD.CARRY", "M3.ADD.CARRY2", "M3.SUB"], lessons.Select(l => l.Competency.Id));
        Assert.Equal([1, 2, 3], lessons.Select(l => l.Competency.Sequence));
        Assert.Equal([1, 2, 2], lessons.Select(l => l.Competency.Chapter));
        Assert.Equal(new TextbookPage("textbook", 1, 7), lessons[0].Competency.TextbookPage);
        Assert.Null(lessons[0].Competency.Practice);
        Assert.Contains(lessons[0].Flags, f => f.StartsWith("The practice rule broke a rule", StringComparison.Ordinal));
        Assert.Contains(lessons[1].Flags, f => f.Contains("already has the ID M3.ADD.CARRY"));
        Assert.Null(lessons[2].Competency.TextbookPage);
        Assert.Contains(lessons[2].Flags, f => f.StartsWith("No printed page", StringComparison.Ordinal));
    }

    [Fact]
    public void The_finished_draft_passes_the_curriculum_rules_with_or_without_prerequisites()
    {
        var outline = new BookOutline([new OutlineChapter(1, "ಎ", "One", [1, 2])], new Dictionary<int, int> { [1] = 1 }, []);
        var lessons = DraftAssembler.Lessons(Book, outline,
            [(outline.Chapters[0], [Extracted("NUM.TENS", [1], null), Extracted("ADD.CARRY", [2], null)])]);

        var draft = DraftAssembler.Finish(outline, lessons, new Dictionary<string, IReadOnlyList<string>> { ["M3.ADD.CARRY"] = ["M3.NUM.TENS"] });
        var without = DraftAssembler.Finish(outline, lessons, null);

        Assert.Equal(["M3.NUM.TENS"], draft.Lessons[1].Competency.Prerequisites);
        Assert.Equal("One", Assert.Single(draft.Chapters).NameEn);
        Assert.All(without.Lessons, l => Assert.Contains(l.Flags, f => f.StartsWith("Prerequisites couldn't", StringComparison.Ordinal)));
    }

    // --- Helpers ----------------------------------------------------------------------------------------------------

    internal static string Json(object value) => JsonSerializer.Serialize(value);

    internal static object OutlinePage(int page, int printed, string kind, bool starts = false, int number = -1, string kn = "", string en = "") =>
        new { page, printedPage = printed, kind, chapterStarts = starts, chapterNumber = number, chapterTitleKn = kn, chapterTitleEn = en };

    internal static object Lesson(
        string key = "ADD.CARRY", string nameKn = "ಕೂಡು", string nameEn = "Addition with carrying", int[]? pages = null,
        object? exercises = null, string description = "The child adds 3-digit numbers, carrying a ten.", int otherPage = 1,
        int picturePage = 1) => new
    {
        key,
        nameKn,
        nameEn,
        nameTe = "కూడిక",
        nameTa = "",
        nameHi = "जोड़",
        keyWordsKn = new[] { "ಕೂಡು", "ಹತ್ತರ ಕಟ್ಟು", "ಕೂಡು" },
        pages = pages ?? [1],
        bookExamples = new[] { "27 + 15 = ?" },
        description,
        examples = new[] { "36 and 47: 6 and 7 make 13, so write 3 and carry a ten." },
        exercises = exercises ?? new { compare = Array.Empty<int[]>(), extremes = Array.Empty<int[]>(), add = Array.Empty<int[]>(), sub = Array.Empty<int[]>() },
        otherExercises = new[] { new { page = otherPage, summary = "Fill in the missing numbers" } },
        pictures = new[] { new { page = picturePage, position = "top", description = "Bundles of ten sticks", teaches = "Tens and ones" } },
        practice = new { kind = "add", digits = 3, carry = "ones", borrow = "none" },
    };

    internal static ExtractedLesson Extracted(string key, IReadOnlyList<int> pages, ExerciseList? exercises) => new(
        key, "ಕೂಡು", "Addition", null, null, null, ["ಕೂಡು"], pages, [], new LessonContentText("Adds.", ["1 + 1 is 2."]),
        exercises, [], [], null, []);
}
