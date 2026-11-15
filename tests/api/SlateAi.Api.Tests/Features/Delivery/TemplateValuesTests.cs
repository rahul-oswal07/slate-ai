using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Delivery;

namespace SlateAi.Api.Tests.Features.Delivery;

/// <summary>The values code puts into Monika's templates (D-98, T-104; <c>docs/whatsapp-setup.md</c> §3).</summary>
public sealed class TemplateValuesTests
{
    private static Competency Lesson(string id, string en, string kn, int page, string book = TextbookPage.Nalikali,
        string? te = null, string? hi = null) =>
        new(id, kn, en, [], null, 0, page, [], new TextbookPage(book, 1, page), NameTe: te, NameHi: hi);

    private static readonly CurriculumGraph Book = new(
    [
        Lesson("M3.CMP", "More-less-equal", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", 4, te: "ఎక్కువ – తక్కువ – సమానం", hi: "ज़्यादा – कम – बराबर"),
        Lesson("M3.PV", "Place value", "ಸ್ಥಾನಬೆಲೆ", 6, te: "స్థాన విలువ", hi: "स्थानीय मान"),
        Lesson("M3.PV2", "Place value again", "ಸ್ಥಾನಬೆಲೆ ೨", 6),
        Lesson("M3.ORD", "Ordering", "ಕ್ರಮ", 9),
        Lesson("M3.ADD", "Addition with carry", "ಸಂಕಲನ", 37, book: "textbook", te: "కూడిక"),
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

    [Fact]
    public void A_lesson_outside_nalikali_gives_nothing_because_the_template_says_nalikali()
    {
        Assert.Null(TemplateValues.Todays("Ravi", "en", ["M3.ADD"], Book));
        Assert.Null(TemplateValues.CatchUp("Ravi", "en", ["M3.CMP", "M3.ADD"], Book));
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
}
