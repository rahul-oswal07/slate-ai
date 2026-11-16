using System.Text.Json;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Features.TodaysMessage;

/// <summary>A longer concept built on the lesson's description and worked examples (T-141, D-123). No database or AI.</summary>
public sealed class TodaysMessageContentTests
{
    private static readonly TodaysPicture Pair = TodaysPicturePicker.Pick([TodaysMessageTests.Compare])!;

    private static readonly Dictionary<string, StoredLessonContent> CarryContent = new()
    {
        [TodaysMessageTests.Carry.Id] = new StoredLessonContent(
            "Add the ones first; 10 ones make 1 ten.",
            ["Adding 296 and 362: ones 6 + 2 = 8, tens 9 + 6 = 15 (write 5, carry 1), hundreds 2 + 3 + 1 = 6, so 658.",
             "A wrong example that prints the picture's answer: 248 + 438 = 686, and a wrong choice 676."],
            "textbook"),
    };

    [Fact]
    public void A9_hears_each_lessons_description_and_examples_and_null_without_them()
    {
        using var json = JsonDocument.Parse(TodaysMessageWriter.UserContent(
            ["kn"], [TodaysMessageTests.Carry, TodaysMessageTests.Compare], null, null, content: CarryContent));

        var lessons = json.RootElement.GetProperty("lessons");
        var carry = lessons[0].GetProperty("content");
        Assert.Equal("Add the ones first; 10 ones make 1 ten.", carry.GetProperty("description").GetString());
        Assert.Equal(2, carry.GetProperty("examples").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, lessons[1].GetProperty("content").ValueKind);
    }

    [Fact]
    public void The_worked_examples_numbers_are_allowed_but_never_the_pictures_answer_or_choices()
    {
        var sum = TodaysPicturePicker.Pick([TodaysMessageTests.Carry], 0, arithmetic: true)!;

        var allowed = TodaysMessageValidator.AllowedNumbers([TodaysMessageTests.Carry], sum, CarryContent);

        Assert.Superset(new HashSet<string> { "296", "362", "658", "15", "248", "438" }, allowed);
        Assert.DoesNotContain("686", allowed);
        Assert.DoesNotContain("676", allowed);
        Assert.DoesNotContain("696", allowed);
    }

    [Fact]
    public void Without_content_the_allowed_numbers_are_as_before()
    {
        Assert.Equal(
            TodaysMessageValidator.AllowedNumbers([TodaysMessageTests.Compare], Pair),
            TodaysMessageValidator.AllowedNumbers([TodaysMessageTests.Compare], Pair, new Dictionary<string, StoredLessonContent>()));
    }

    [Theory]
    [InlineData(6, 140, true)]
    [InlineData(6, 141, false)]
    [InlineData(7, 70, false)]
    public void The_concept_may_be_six_sentences_and_140_words(int sentences, int words, bool passes)
    {
        var concept = Concept(sentences, words);
        var text = new TodaysText(concept, ["ಕಡ್ಡಿಗಳ ಕಟ್ಟು ಮಾಡಿ.", "ಯಾವುದು ಹೆಚ್ಚು ಎಂದು ಮಗು ಹೇಳಲಿ."]);

        var result = TodaysMessageValidator.Check("kn", text, TodaysMessageValidator.AllowedNumbers([TodaysMessageTests.Compare], Pair), []);

        Assert.Equal(passes, result is null);
        if (!passes)
        {
            Assert.Equal("fallback:too-long", result!.Verdict);
        }
    }

    /// <summary>Kannada words spread over the sentences, each sentence ending in a full stop.</summary>
    private static string Concept(int sentences, int words)
    {
        var perSentence = Enumerable.Range(0, sentences).Select(i => words / sentences + (i < words % sentences ? 1 : 0));
        return string.Join(' ', perSentence.Select(n => string.Join(' ', Enumerable.Repeat("ಕಟ್ಟು", n)) + "."));
    }
}
