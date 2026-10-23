using System.Text.Json;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.CatchUps.Script;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class A3ValidatorTests
{
    internal static readonly CurriculumGraph Book = new(
    [
        new Competency("M3.SUB.BORROW", "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ", "3-digit subtraction with borrowing", [], null, 5, 15, [],
            new TextbookPage("textbook", 2, 15), KeyWordsKn: ["ಕಳೆ", "ದಶಕ", "ಹತ್ತು", "ಬಿಡಿ"],
            Practice: new PracticeRule("sub", 3, Borrow: "tens")),
    ]);

    internal static readonly ScriptInput Input = new(
        "te", ["M3.SUB.BORROW"], PackagePractice.For(["M3.SUB.BORROW"], Book, "seed")!, ["43 − 18"]);

    private static readonly string[] Names = ["Ravi", "Ananya", "Shobha"];

    internal static string Output(
        string script = "ఈ రోజు మనం ಕಳೆ నేర్చుకుందాం. ಕಳೆ అంటే తీసివేత. ಹತ್ತು నుండి ఒక ದಶಕ తీసుకుందాం. ದಶಕ ಹತ್ತು. మీ టీచర్‌కు చూపించండి.",
        string[]? keyWords = null,
        string instruction = "ఒక కట్టను విప్పి 10 పుల్లలు చేయండి.",
        string glossKn = "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ. ನಿಮ್ಮ ಟೀಚರ್‌ಗೆ ತೋರಿಸು",
        string[]? objects = null) =>
        JsonSerializer.Serialize(new
        {
            script,
            keyWordsKn = keyWords ?? ["ಕಳೆ", "ದಶಕ", "ಹತ್ತು"],
            activity = new { instruction, objectOption = "పుల్లలు లేకపోతే రాళ్ళు వాడండి.", objects = objects ?? ["stones"] },
            checkPrompt = "ఏ బొమ్మ సరైనది?",
            glossKn,
            glossEn = "Subtraction with borrowing. Show your teacher.",
        });

    [Fact]
    public void Valid_output_is_kept_trimmed()
    {
        var result = A3Validator.Validate(Output(), Input, Book, Names);

        Assert.Equal("valid", result.Verdict);
        Assert.Equal(["ಕಳೆ", "ದಶಕ", "ಹತ್ತು"], result.Content!.KeyWordsKn);
        Assert.StartsWith("ఈ రోజు", result.Content.Script);
        Assert.Equal(["stones"], result.Content.Objects);
    }

    [Theory]
    [InlineData("not json", "fallback:invalid-output")]
    [InlineData("""{"script":"x"}""", "fallback:invalid-output")]
    public void Unreadable_or_incomplete_output_is_refused(string json, string verdict)
    {
        Assert.Equal(verdict, A3Validator.Validate(json, Input, Book, Names).Verdict);
    }

    [Fact]
    public void Key_words_must_be_three_distinct_words_from_the_lessons_list()
    {
        Assert.Equal("fallback:key-words", A3Validator.Validate(Output(keyWords: ["ಕಳೆ", "ದಶಕ"]), Input, Book, Names).Verdict);
        Assert.Equal("fallback:key-words", A3Validator.Validate(Output(keyWords: ["ಕಳೆ", "ದಶಕ", "ಹತ್ತು", "ಬಿಡಿ"]), Input, Book, Names).Verdict);
        Assert.Equal("fallback:key-words", A3Validator.Validate(Output(keyWords: ["ಕಳೆ", "ಕಳೆ", "ದಶಕ"]), Input, Book, Names).Verdict);
        Assert.Equal("fallback:key-words", A3Validator.Validate(Output(keyWords: ["ಕಳೆ", "ದಶಕ", "ನೂರು"]), Input, Book, Names).Verdict);
    }

    [Theory]
    [InlineData("Hello {child}, today we subtract.")]
    [InlineData("Show {teacher} when you come back.")]
    [InlineData("<b>Subtract</b>")]
    public void Placeholders_and_markup_are_refused(string script)
    {
        Assert.Equal("fallback:placeholder", A3Validator.Validate(Output(script: script), Input, Book, Names).Verdict);
    }

    [Theory]
    [InlineData("Ravi, let us subtract today.")]
    [InlineData("Show SHOBHA teacher when you come back.")]
    public void A_name_from_the_class_is_refused(string script)
    {
        Assert.Equal("fallback:name", A3Validator.Validate(Output(script: script), Input, Book, Names).Verdict);
    }

    [Fact]
    public void A_long_script_in_an_indic_script_counts_words_not_characters()
    {
        // 120 Tamil words are well over 600 characters; that is fine.
        var script = "ಕಳೆ ದಶಕ ಹತ್ತು " + string.Join(' ', Enumerable.Repeat("கழித்தலைச்", 117));

        Assert.Equal("valid", A3Validator.Validate(Output(script: script), Input, Book, Names).Verdict);
    }

    [Fact]
    public void Too_long_is_refused()
    {
        var words = string.Join(' ', Enumerable.Repeat("పదం", A3Validator.MaxScriptWords + 1));

        Assert.Equal("fallback:too-long", A3Validator.Validate(Output(script: words), Input, Book, Names).Verdict);
        Assert.Equal("fallback:too-long", A3Validator.Validate(Output(glossKn: new string('ಕ', 700)), Input, Book, Names).Verdict);
    }

    [Fact]
    public void Schema_limits_key_words_to_the_lessons_list()
    {
        var schema = A3Validator.Schema(A3Validator.KeyWordChoices(Input, Book));

        var choices = schema.GetProperty("properties").GetProperty("keyWordsKn").GetProperty("items").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString());
        Assert.Equal(["ಕಳೆ", "ದಶಕ", "ಹತ್ತು", "ಬಿಡಿ"], choices);
    }
}
