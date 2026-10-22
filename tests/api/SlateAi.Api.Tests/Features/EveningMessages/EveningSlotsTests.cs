using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Tests.Features.EveningMessages;

public sealed class EveningSlotsTests
{
    private static readonly LanguageWording English = new(
        "We missed {child} today. In class we learned {lesson}. Homework: textbook {page}. A classmate is keeping {child}’s seat.",
        "Part {part}, page {page}",
        "page {page}");

    private static readonly LanguageWording Telugu = new(
        "ఈ రోజు {child} మాకు గుర్తొచ్చాడు. ఈ రోజు తరగతిలో {lesson} నేర్చుకున్నాం. ఇంటి పని: పాఠ్యపుస్తకం {page}.",
        "భాగం {part}, పేజీ {page}",
        "పేజీ {page}");

    private static readonly CurriculumGraph Book = new(
        [
            new Competency("M3.SUB.BORROW", "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ", "subtraction with borrowing", [], null, 5, 4, [],
                new TextbookPage("textbook", 2, 15), NameTe: "పది తీసుకుని తీసివేత"),
            new Competency("M3.SUB.CHECK", "ತಾಳೆ", "checking subtraction", [], null, 5, 3, [],
                new TextbookPage("textbook", null, 4), NameTe: "సరిచూడటం"),
            new Competency("M3.WORKBOOK", "ಕ", "workbook only", [], null, 5, 5, [],
                new TextbookPage("workbook", 1, 3), NameTe: "వర్క్‌బుక్"),
        ],
        []);

    [Fact]
    public void Fills_the_name_lesson_and_textbook_page()
    {
        var filled = EveningSlots.Fill(English, "en", "Ravi", ["M3.SUB.BORROW"], Book);

        Assert.Equal(
            "We missed Ravi today. In class we learned subtraction with borrowing. Homework: textbook Part 2, page 15. A classmate is keeping Ravi’s seat.",
            filled!.Text);
        Assert.Equal(new SlotValues("subtraction with borrowing", "Part 2, page 15"), filled.Slots);
    }

    [Fact]
    public void Uses_the_familys_language_for_the_lesson_and_the_page()
    {
        var filled = EveningSlots.Fill(Telugu, "te", "Ravi", ["M3.SUB.BORROW"], Book);

        Assert.Contains("పది తీసుకుని తీసివేత", filled!.Text);
        Assert.Contains("భాగం 2, పేజీ 15", filled.Text);
    }

    [Fact]
    public void Several_lessons_are_joined_and_the_page_is_the_first_ones()
    {
        var filled = EveningSlots.Fill(English, "en", "Ravi", ["M3.SUB.CHECK", "M3.SUB.BORROW"], Book);

        Assert.Equal("checking subtraction, subtraction with borrowing", filled!.Slots.Lesson);
        Assert.Equal("page 4", filled.Slots.Page);
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

        Assert.Equal(["en", "kn", "ta", "te"], wording.Languages.Keys.Order());
        foreach (var (language, text) in wording.Languages)
        {
            Assert.Contains("{child}", text.Text);
            Assert.Contains("{lesson}", text.Text);
            Assert.Contains("{page}", text.Text);
            Assert.DoesNotContain("{tomorrow}", text.Text); // D-48
            Assert.DoesNotContain("{buddy}", text.Text);    // D-29

            // Every lesson in the book can be named in every language (te/ta are drafts for review, T-26).
            foreach (var lesson in book.All)
            {
                Assert.NotNull(EveningSlots.Fill(text, language, "Ravi", [lesson.Id], book));
            }
        }
    }
}
