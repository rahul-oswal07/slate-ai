using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Tests.Features.EveningMessages;

public sealed class EveningSlotsTests
{
    private static readonly LanguageWording English = new(
        "We missed {child} today. In class we learned {lesson}. Homework: {page}. A classmate is keeping {child}’s seat.",
        "{book} Part {part}, page {page}",
        "{book} page {page}",
        new Dictionary<string, string> { ["textbook"] = "textbook", [TextbookPage.Nalikali] = "Nalikali Maths" });

    private static readonly LanguageWording Telugu = new(
        "ఈ రోజు {child} మాకు గుర్తొచ్చాడు. ఈ రోజు తరగతిలో {lesson} నేర్చుకున్నాం. ఇంటి పని: {page}.",
        "{book} భాగం {part}, పేజీ {page}",
        "{book} పేజీ {page}",
        new Dictionary<string, string> { ["textbook"] = "పాఠ్యపుస్తకం", [TextbookPage.Nalikali] = "నలి-కలి గణితం" });

    /// <summary>Wording v3, as rows approved before T-101 hold it: "textbook" in the text, no {book} in the page.</summary>
    private static readonly LanguageWording EnglishV3 = new(
        "We missed {child} today. In class we learned {lesson}. Homework: textbook {page}. A classmate is keeping {child}’s seat.",
        "Part {part}, page {page}",
        "page {page}");

    private static readonly CurriculumGraph Book = new(
        [
            new Competency("M3.SUB.BORROW", "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ", "subtraction with borrowing", [], null, 5, 4, [],
                new TextbookPage("textbook", 2, 15), NameTe: "పది తీసుకుని తీసివేత"),
            new Competency("M3.SUB.MAKE", "ಸಮಸ್ಯಾ ರಚನೆ", "making subtraction sums", [], null, 5, 3, [],
                new TextbookPage("textbook", null, 4), NameTe: "సరిచూడటం"),
            new Competency("M3.WORKBOOK", "ಕ", "workbook only", [], null, 5, 5, [],
                new TextbookPage("workbook", 1, 3), NameTe: "వర్క్‌బుక్"),
            new Competency("M3.BRIDGE.COMPARE", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "more, less, equal", [], null, 0, 0, [],
                new TextbookPage(TextbookPage.Nalikali, 1, 4)),
        ],
        []);

    [Fact]
    public void Fills_the_name_lesson_and_textbook_page()
    {
        var filled = EveningSlots.Fill(English, "en", "Ravi", ["M3.SUB.BORROW"], Book);

        Assert.Equal(
            "We missed Ravi today. In class we learned subtraction with borrowing. Homework: textbook Part 2, page 15. A classmate is keeping Ravi’s seat.",
            filled!.Text);
        Assert.Equal(new SlotValues("subtraction with borrowing", "textbook Part 2, page 15"), filled.Slots);
    }

    [Fact]
    public void Uses_the_familys_language_for_the_lesson_and_the_page()
    {
        var filled = EveningSlots.Fill(Telugu, "te", "Ravi", ["M3.SUB.BORROW"], Book);

        Assert.Contains("పది తీసుకుని తీసివేత", filled!.Text);
        Assert.Contains("ఇంటి పని: పాఠ్యపుస్తకం భాగం 2, పేజీ 15.", filled.Text);
    }

    [Fact]
    public void Several_lessons_are_joined_and_the_page_is_the_first_ones()
    {
        var filled = EveningSlots.Fill(English, "en", "Ravi", ["M3.SUB.MAKE", "M3.SUB.BORROW"], Book);

        Assert.Equal("making subtraction sums, subtraction with borrowing", filled!.Slots.Lesson);
        Assert.Equal("textbook page 4", filled.Slots.Page);
    }

    [Fact]
    public void A_nalikali_page_names_nalikali_not_the_textbook()
    {
        // T-101, D-99: code writes the book into the page slot, so a Nalikali page never reads "textbook" (J4).
        var filled = EveningSlots.Fill(English, "en", "Ravi", ["M3.BRIDGE.COMPARE"], Book);

        Assert.Equal("Nalikali Maths Part 1, page 4", filled!.Slots.Page);
        Assert.Contains("Homework: Nalikali Maths Part 1, page 4.", filled.Text);
        Assert.DoesNotContain("textbook", filled.Text);
    }

    [Fact]
    public void A_book_the_wording_has_no_name_for_gives_no_message()
    {
        var noNalikali = English with { Books = new Dictionary<string, string> { ["textbook"] = "textbook" } };

        Assert.Null(EveningSlots.Fill(noNalikali, "en", "Ravi", ["M3.BRIDGE.COMPARE"], Book));
        Assert.Null(EveningSlots.Fill(English with { Books = null }, "en", "Ravi", ["M3.SUB.BORROW"], Book));
    }

    [Fact]
    public void Wording_approved_before_the_book_slot_still_fills()
    {
        var filled = EveningSlots.Fill(EnglishV3, "en", "Ravi", ["M3.BRIDGE.COMPARE"], Book);

        Assert.Equal("Part 1, page 4", filled!.Slots.Page);
        Assert.Contains("Homework: textbook Part 1, page 4.", filled.Text);
    }

    [Theory]
    [InlineData("ta", "M3.SUB.BORROW")] // no Tamil name
    [InlineData("en", "M9.NOT.REAL")]   // unknown lesson
    [InlineData("en", "M3.WORKBOOK")]   // the message says "textbook"
    public void Anything_missing_gives_no_message_rather_than_a_half_filled_one(string language, string lesson)
    {
        Assert.Null(EveningSlots.Fill(English, language, "Ravi", [lesson], Book));
    }

    [Fact]
    public void No_lessons_gives_no_message()
    {
        Assert.Null(EveningSlots.Fill(English, "en", "Ravi", [], Book));
    }

    [Fact]
    public void Render_marks_each_filled_slot_for_the_teachers_example()
    {
        var parts = EveningSlots.Render("Hi {child}, {lesson} on {page}. {unknown}", "Ravi", new SlotValues("adding", "page 4")).Parts;

        Assert.Equal(
            [
                new TextPart("Hi ", null),
                new TextPart("Ravi", "child"),
                new TextPart(", ", null),
                new TextPart("adding", "lesson"),
                new TextPart(" on ", null),
                new TextPart("page 4", "page"),
                new TextPart(". {unknown}", null),
            ],
            parts);
    }

    [Fact]
    public void The_wording_file_has_every_slot_in_every_language()
    {
        var wording = EveningWording.Load(Path.Combine(AppContext.BaseDirectory, "data/seed/evening-message.json"));
        var book = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data/curriculum"));

        Assert.Equal(["en", "hi", "kn", "ta", "te"], wording.Languages.Keys.Order());
        Assert.DoesNotContain("గుర్తొచ్చాడు", wording.Languages["te"].Text); // masculine verb replaced (D-72)
        foreach (var (language, text) in wording.Languages)
        {
            Assert.Contains("{child}", text.Text);
            Assert.Contains("{lesson}", text.Text);
            Assert.Contains("{page}", text.Text);
            Assert.DoesNotContain("{tomorrow}", text.Text); // D-48
            Assert.DoesNotContain("{buddy}", text.Text);    // D-29

            // T-101, D-99: the book is named by code inside the page slot, never as a fixed word in the text.
            Assert.Contains("{book}", text.Page);
            Assert.Contains("{book}", text.PageNoPart);
            Assert.False(string.IsNullOrWhiteSpace(text.Books?.GetValueOrDefault("textbook")));
            Assert.False(string.IsNullOrWhiteSpace(text.Books?.GetValueOrDefault(TextbookPage.Nalikali)));
            Assert.DoesNotContain(text.Books!["textbook"], text.Text);

            // Every lesson in the book can be named in every language (te/ta/hi are drafts for review, T-26).
            foreach (var lesson in book.All)
            {
                Assert.NotNull(EveningSlots.Fill(text, language, "Ravi", [lesson.Id], book));
            }
        }
    }

    [Fact]
    public void The_kannada_wording_never_glues_a_suffix_to_the_childs_name()
    {
        // T-101: names are stored in Latin letters, so "{child}ಯ" read "Manjunathಯ" on the family's phone.
        var kannada = EveningWording.Load(Path.Combine(AppContext.BaseDirectory, "data/seed/evening-message.json")).Languages["kn"];

        Assert.DoesNotMatch(@"\{child\}[\p{L}\p{M}]", kannada.Text);
        Assert.Contains("Manjunath ಅವರ ಜಾಗ", EveningSlots.Render(kannada.Text, "Manjunath", new SlotValues("x", "y")).Text);
    }

    [Fact]
    public void The_kannada_evening_text_for_the_demo_lesson_names_nalikali()
    {
        var kannada = EveningWording.Load(Path.Combine(AppContext.BaseDirectory, "data/seed/evening-message.json")).Languages["kn"];
        var book = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data/curriculum"));

        var filled = EveningSlots.Fill(kannada, "kn", "Manjunath", ["M3.BRIDGE.COMPARE"], book);

        Assert.Contains("ಮನೆಕೆಲಸ: ನಲಿ-ಕಲಿ ಗಣಿತ ಭಾಗ 1, ಪುಟ 4.", filled!.Text);
        Assert.DoesNotContain("ಪಠ್ಯಪುಸ್ತಕ", filled.Text);
    }
}
