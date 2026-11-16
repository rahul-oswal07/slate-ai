using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Delivery;

namespace SlateAi.Api.Tests.Features.Delivery;

/// <summary>The values code puts into Monika's templates (D-98, T-104; <c>docs/whatsapp-setup.md</c> §3).</summary>
public sealed class TemplateValuesTests
{
    private static Competency Lesson(string id, string en, string kn, int page, string book = TextbookPage.Nalikali,
        string? te = null, string? hi = null, int part = 1, string? ta = null) =>
        new(id, kn, en, [], null, 0, page, [], new TextbookPage(book, part, page), NameTe: te, NameTa: ta, NameHi: hi);

    private static readonly CurriculumGraph Book = new(
    [
        Lesson("M3.CMP", "More-less-equal", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", 4, te: "ఎక్కువ – తక్కువ – సమానం", hi: "ज़्यादा – कम – बराबर"),
        Lesson("M3.PV", "Place value", "ಸ್ಥಾನಬೆಲೆ", 6, te: "స్థాన విలువ", hi: "स्थानीय मान"),
        Lesson("M3.PV2", "Place value again", "ಸ್ಥಾನಬೆಲೆ ೨", 6),
        Lesson("M3.ORD", "Ordering", "ಕ್ರಮ", 9),
        Lesson("M3.ADD", "Addition with carry", "ಸಂಕಲನ", 37, book: "textbook", te: "కూడిక", hi: "जोड़", ta: "கூட்டல்"),
        Lesson("M3.SUB", "Subtraction", "ವ್ಯವಕಲನ", 40, book: "textbook"),
        Lesson("M3.DIV", "Sharing", "ಹಂಚಿಕೆ", 5, book: "textbook", part: 2),
        Lesson("M3.WB", "Workbook page", "ಕಾರ್ಯಪುಸ್ತಕ", 3, book: "workbook"),
    ]);

    [Theory]
    [InlineData("en", "More-less-equal")]
    [InlineData("kn", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ")]
    [InlineData("hi", "ज़्यादा – कम – बराबर")]
    public void Todays_message_gives_child_lesson_and_the_bare_page_number(string language, string lesson)
    {
        Assert.Equal(["Ravi", lesson, "4"], TemplateValues.Todays("Ravi", language, ["M3.CMP"], Book));
    }

    [Fact]
    public void Todays_message_with_two_lessons_names_both_and_takes_the_first_page()
    {
        Assert.Equal(
            ["Ravi", "Place value and More-less-equal", "6"],
            TemplateValues.Todays("Ravi", "en", ["M3.PV", "M3.CMP"], Book));
    }

    [Theory]
    [InlineData("en", "More-less-equal", "page 4")]
    [InlineData("kn", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "ಪುಟ 4")]
    [InlineData("te", "ఎక్కువ – తక్కువ – సమానం", "పేజీ 4")]
    [InlineData("hi", "ज़्यादा – कम – बराबर", "पृष्ठ 4")]
    public void Catch_up_with_one_lesson_reads_page_in_the_singular(string language, string lessons, string pages)
    {
        Assert.Equal(["Ravi", lessons, pages], TemplateValues.CatchUp("Ravi", language, ["M3.CMP"], Book));
    }

    [Theory]
    [InlineData("en", "More-less-equal and Place value", "pages 4 and 6")]
    [InlineData("kn", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ ಮತ್ತು ಸ್ಥಾನಬೆಲೆ", "ಪುಟ 4 ಮತ್ತು 6")]
    [InlineData("te", "ఎక్కువ – తక్కువ – సమానం, స్థాన విలువ", "పేజీలు 4, 6")]
    [InlineData("hi", "ज़्यादा – कम – बराबर और स्थानीय मान", "पृष्ठ 4 और 6")]
    public void Catch_up_with_two_lessons_names_both_and_both_pages(string language, string lessons, string pages)
    {
        Assert.Equal(["Ravi", lessons, pages], TemplateValues.CatchUp("Ravi", language, ["M3.CMP", "M3.PV"], Book));
    }

    [Fact]
    public void Catch_up_pages_are_listed_once_and_in_order()
    {
        Assert.Equal(
            ["Ravi", "Ordering, Place value and Place value again", "pages 6 and 9"],
            TemplateValues.CatchUp("Ravi", "en", ["M3.ORD", "M3.PV", "M3.PV2"], Book));
        Assert.Equal("page 6", TemplateValues.CatchUp("Ravi", "en", ["M3.PV", "M3.PV2"], Book)![2]);
    }

    // T-132, D-119: the templates' fixed text says "Nalikali Maths"; a textbook lesson names its book after the page.
    [Theory]
    [InlineData("en", "Addition with carry", "37 (Maths textbook)")]
    [InlineData("kn", "ಸಂಕಲನ", "37 (ಗಣಿತ ಪಠ್ಯಪುಸ್ತಕ)")]
    [InlineData("te", "కూడిక", "37 (గణిత పాఠ్యపుస్తకం)")]
    [InlineData("ta", "கூட்டல்", "37 (கணிதப் பாடநூல்)")]
    [InlineData("hi", "जोड़", "37 (गणित पाठ्यपुस्तक)")]
    public void Todays_message_for_a_textbook_lesson_names_the_book_after_the_page(string language, string lesson, string page)
    {
        Assert.Equal(["Ravi", lesson, page], TemplateValues.Todays("Ravi", language, ["M3.ADD"], Book));
    }

    [Fact]
    public void A_textbook_lesson_in_part_2_says_so()
    {
        Assert.Equal("5 (Maths textbook Part 2)", TemplateValues.Todays("Ravi", "en", ["M3.DIV"], Book)![2]);
        Assert.Equal("ಪುಟ 5 (ಗಣಿತ ಪಠ್ಯಪುಸ್ತಕ ಭಾಗ 2)", TemplateValues.CatchUp("Ravi", "kn", ["M3.DIV"], Book)![2]);
    }

    [Fact]
    public void Catch_up_in_the_textbook_names_the_book_once_after_its_pages()
    {
        Assert.Equal("page 37 (Maths textbook)", TemplateValues.CatchUp("Ravi", "en", ["M3.ADD"], Book)![2]);
        Assert.Equal("pages 37 and 40 (Maths textbook)", TemplateValues.CatchUp("Ravi", "en", ["M3.SUB", "M3.ADD"], Book)![2]);
    }

    [Fact]
    public void Catch_up_across_books_gives_the_nalikali_pages_first_then_each_other_books_pages()
    {
        Assert.Equal(
            ["Ravi", "More-less-equal and Addition with carry", "page 4 and page 37 (Maths textbook)"],
            TemplateValues.CatchUp("Ravi", "en", ["M3.CMP", "M3.ADD"], Book));
        Assert.Equal(
            "pages 4 and 6, pages 37 and 40 (Maths textbook) and page 5 (Maths textbook Part 2)",
            TemplateValues.CatchUp("Ravi", "en", ["M3.DIV", "M3.SUB", "M3.PV", "M3.ADD", "M3.CMP"], Book)![2]);
    }

    [Fact]
    public void A_lesson_in_a_book_without_a_name_in_the_templates_gives_nothing()
    {
        Assert.Null(TemplateValues.Todays("Ravi", "en", ["M3.WB"], Book));
        Assert.Null(TemplateValues.CatchUp("Ravi", "en", ["M3.CMP", "M3.WB"], Book));
    }

    [Fact]
    public void A_name_missing_in_the_family_language_gives_nothing()
    {
        // M3.ORD has no Telugu name.
        Assert.Null(TemplateValues.Todays("Ravi", "te", ["M3.ORD"], Book));
        Assert.Null(TemplateValues.CatchUp("Ravi", "ta", ["M3.CMP"], Book));
    }

    [Theory]
    [InlineData("xx", "M3.CMP")]
    [InlineData("en", "M3.UNKNOWN")]
    public void An_unknown_language_or_lesson_gives_nothing(string language, string id)
    {
        Assert.Null(TemplateValues.Todays("Ravi", language, [id], Book));
        Assert.Null(TemplateValues.CatchUp("Ravi", language, [id], Book));
    }

    [Fact]
    public void No_lessons_gives_nothing()
    {
        Assert.Null(TemplateValues.Todays("Ravi", "en", [], Book));
        Assert.Null(TemplateValues.CatchUp("Ravi", "en", [], Book));
    }

    [Fact]
    public void The_real_curriculum_fills_the_demo_lesson_in_every_template_language()
    {
        // The demo story's lesson (Nalikali Part 1 p 4) must fill both templates in all five languages.
        var curriculum = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));
        var demo = curriculum.All.Single(c => c.TextbookPage?.Book == TextbookPage.Nalikali && c.TextbookPage.Page == 4);

        foreach (var language in new[] { "en", "kn", "te", "ta", "hi" })
        {
            Assert.Equal("4", TemplateValues.Todays("Ravi", language, [demo.Id], curriculum)?[2]);
            Assert.NotNull(TemplateValues.CatchUp("Ravi", language, [demo.Id], curriculum));
        }
    }

    [Fact]
    public void The_real_curriculum_fills_every_lesson_with_a_todays_message_in_every_template_language()
    {
        // T-128's add and subtract lessons (and every other lesson with the book's exercises) are in the textbook.
        var curriculum = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));
        var withExercises = curriculum.All.Where(c => c.Exercises is not null).ToList();
        Assert.Contains(withExercises, c => c.TextbookPage?.Book == "textbook");

        foreach (var lesson in withExercises)
        {
            foreach (var language in new[] { "en", "kn", "te", "ta", "hi" })
            {
                Assert.NotNull(TemplateValues.Todays("Ravi", language, [lesson.Id], curriculum));
                Assert.NotNull(TemplateValues.CatchUp("Ravi", language, [lesson.Id], curriculum));
            }
        }
    }
}
