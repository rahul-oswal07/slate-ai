using System.Text.Json;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Features.TodaysMessage;

/// <summary>Today's message logic that needs no database or AI (T-88): the picture pair, A9's checks, the family view.</summary>
public sealed class TodaysMessageTests
{
    /// <summary>Nalikali Part 1 p 4, as in the curriculum file (T-87).</summary>
    internal static readonly Competency Compare = new(
        "M3.BRIDGE.COMPARE", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "More, less, equal: comparing 2-digit numbers", [], null, 0, 0,
        ["48 ☐ 33: >, < or =", "3, 38, 64, 24: circle the smallest, square the biggest"],
        new TextbookPage(TextbookPage.Nalikali, 1, 4),
        NameHi: "ज़्यादा – कम – बराबर",
        KeyWordsKn: ["ಹೆಚ್ಚು", "ಕಡಿಮೆ", "ಸಮ"],
        Exercises: new ExerciseList(
            Compare: [[48, 33], [48, 84], [75, 57], [86, 86]],
            Extremes: [[3, 38, 64, 24]]));

    internal static readonly Competency NoExercises = new(
        "M3.ADD.CARRY", "ದಶಕ ಸಹಿತ ಸಂಕಲನ", "Addition with carrying", [], null, 4, 3, ["456 + 278"], new TextbookPage("textbook", 1, 80),
        Practice: new PracticeRule("add", 3, Carry: "ones"));

    internal const string Kannada =
        """{"language":"kn","concept":"ಹೋಲಿಸುವಾಗ ಮೊದಲು ಹತ್ತರ ಕಟ್ಟುಗಳನ್ನು ನೋಡಿ. ಕಟ್ಟುಗಳು ಸಮವಾಗಿದ್ದರೆ ಬಿಡಿ ಕಡ್ಡಿಗಳನ್ನು ನೋಡಿ. ಹೆಚ್ಚು ಕಟ್ಟು ಇರುವ ಸಂಖ್ಯೆ ಹೆಚ್ಚು.","activity":["75 ಮತ್ತು 57 ಕ್ಕೆ ಕಡ್ಡಿಗಳ ಕಟ್ಟು ಮಾಡಿ.","ಯಾವುದರಲ್ಲಿ ಹೆಚ್ಚು ಕಟ್ಟು ಇದೆ ಎಂದು ಮಗುವನ್ನು ಕೇಳಿ.","ಯಾವುದು ಹೆಚ್ಚು, ಯಾವುದು ಕಡಿಮೆ ಎಂದು ಮಗು ಹೇಳಲಿ."]}""";

    internal const string Hindi =
        """{"language":"hi","concept":"पहले दस की गड्डियाँ गिनिए। गड्डियाँ बराबर हों तो खुली तीलियाँ गिनिए। जिसमें ज़्यादा गड्डियाँ हैं, वह संख्या बड़ी है।","activity":["75 और 57 के लिए तीलियों की गड्डियाँ बनाइए।","बच्चे से पूछिए किसमें ज़्यादा गड्डियाँ हैं।","बच्चा बोले कौन ज़्यादा है और कौन कम।"]}""";

    internal static string Output(params string[] languages) => $$"""{"languages":[{{string.Join(',', languages)}}]}""";

    private static readonly TodaysPicture Pair = TodaysPicturePicker.Pick([Compare])!;

    [Fact]
    public void The_picture_pair_is_the_books_first_with_the_answer_by_code()
    {
        Assert.Equal(new TodaysPicture("compare", 48, 33, ">"), Pair);
        Assert.Equal("<", TodaysPicturePicker.Pick([Compare], 1)!.Answer);
        Assert.Equal(new TodaysPicture("compare", 86, 86, "="), TodaysPicturePicker.Pick([Compare], 3));
        Assert.Equal(Pair, TodaysPicturePicker.Pick([Compare], 4)); // wraps round
    }

    [Fact]
    public void Only_a_lesson_with_the_books_exercises_gets_a_message()
    {
        Assert.True(TodaysPicturePicker.Supported([Compare]));
        Assert.False(TodaysPicturePicker.Supported([NoExercises]));
        Assert.Equal(Pair, TodaysPicturePicker.Pick([NoExercises, Compare]));
    }

    [Fact]
    public void Valid_output_in_every_language_passes()
    {
        var result = Validate(Output(Kannada, Hindi), ["kn", "hi"]);

        Assert.Equal("valid", result.Verdict);
        Assert.Equal(3, result.Texts!["kn"].Steps.Count);
        Assert.StartsWith("पहले", result.Texts["hi"].Concept);
    }

    [Theory]
    [InlineData("missing language", "fallback:invalid-output")]
    [InlineData("extra language", "fallback:invalid-output")]
    [InlineData("not json", "fallback:invalid-output")]
    [InlineData("a name", "fallback:name")]
    [InlineData("a sign", "fallback:placeholder")]
    [InlineData("a made-up number", "fallback:numbers")]
    [InlineData("four steps", "fallback:too-long")]
    [InlineData("four sentences", "fallback:too-long")]
    [InlineData("wrong script", "fallback:script")]
    public void Bad_output_is_refused_never_passed_through(string problem, string verdict)
    {
        var json = problem switch
        {
            "missing language" => Output(Kannada),
            "extra language" => Output(Kannada, Hindi, Hindi.Replace("\"hi\"", "\"te\"")),
            "not json" => "{\"languages\":",
            "a name" => Output(Kannada.Replace("ಮಗುವನ್ನು ಕೇಳಿ", "Ravi ಕೇಳಿ"), Hindi),
            "a sign" => Output(Kannada.Replace("75 ಮತ್ತು 57", "75 > 57"), Hindi),
            "a made-up number" => Output(Kannada.Replace("75 ಮತ್ತು 57", "73 ಮತ್ತು 57"), Hindi),
            "four steps" => Output(Kannada.Replace("\"]}", "\",\"ಮತ್ತೆ ಮಾಡಿ.\"]}"), Hindi),
            "four sentences" => Output(Kannada.Replace("ಸಂಖ್ಯೆ ಹೆಚ್ಚು.\"", "ಸಂಖ್ಯೆ ಹೆಚ್ಚು. ಚೆನ್ನಾಗಿದೆ.\""), Hindi),
            "wrong script" => Output(Kannada, Hindi.Replace("पहले दस की गड्डियाँ गिनिए।", "First count the bundles of ten and then the loose sticks.")),
            _ => throw new ArgumentOutOfRangeException(nameof(problem)),
        };

        var result = Validate(json, ["kn", "hi"]);

        Assert.Equal(verdict, result.Verdict);
        Assert.Null(result.Texts);
    }

    [Fact]
    public void The_books_numbers_their_tens_and_ones_and_ten_are_the_only_allowed_numbers()
    {
        var allowed = TodaysMessageValidator.AllowedNumbers([Compare], Pair);

        Assert.Superset(new HashSet<string> { "48", "33", "4", "8", "3", "10", "75", "7", "5", "57", "86", "64" }, allowed);
        Assert.DoesNotContain("73", allowed);
        Assert.DoesNotContain("100", allowed);
    }

    [Fact]
    public void The_request_carries_curriculum_and_code_numbers_only()
    {
        var content = TodaysMessageWriter.UserContent(["kn", "hi"], [Compare], Pair, ["output: fallback:numbers"]);
        using var json = JsonDocument.Parse(content);

        Assert.Equal(48, json.RootElement.GetProperty("pictureQuestion").GetProperty("a").GetInt32());
        Assert.False(json.RootElement.GetProperty("pictureQuestion").TryGetProperty("answer", out _));
        Assert.Contains("ಹೆಚ್ಚು", content);
        Assert.Contains("fallback:numbers", content);
    }

    [Fact]
    public void A_family_gets_its_own_language_and_nothing_she_removed()
    {
        var sections = Sections().WithRemoved(TodaysSectionKind.Activity, true)!;

        var view = TodaysFamilyView.For(sections, "hi");

        Assert.Equal("हिंदी", view.Concept);
        Assert.Null(view.Steps);
        Assert.Equal(Pair, view.Picture);
        Assert.Null(view.Video);
        Assert.Null(view.Note);
    }

    [Fact]
    public void The_last_section_cannot_be_removed_and_unknown_ones_are_refused()
    {
        var two = Sections().WithRemoved(TodaysSectionKind.Concept, true)!.WithRemoved(TodaysSectionKind.Activity, true)!;

        Assert.False(two.WithRemoved(TodaysSectionKind.Picture, true)!.HasAnySection);
        Assert.Null(two.WithRemoved(TodaysSectionKind.Video, true)); // no video until T-94
        Assert.Null(two.WithRemoved("greeting", true));
    }

    [Fact]
    public void Sections_round_trip_as_json_and_a_generating_row_has_none()
    {
        var sections = Sections();

        Assert.Equivalent(sections, TodaysSections.Parse(sections.ToJson()));
        Assert.Null(TodaysSections.Parse("null"));
    }

    internal static TodaysSections Sections() => new(
        new TodaysConcept(
            new Dictionary<string, string> { ["kn"] = "ಕನ್ನಡ", ["hi"] = "हिंदी" },
            new Dictionary<string, Guid> { ["kn"] = Guid.NewGuid(), ["hi"] = Guid.NewGuid() }),
        new TodaysActivity(new Dictionary<string, IReadOnlyList<string>> { ["kn"] = ["ಒಂದು"], ["hi"] = ["एक"] }),
        Pair);

    private static TodaysWriteResult Validate(string json, IReadOnlyList<string> languages) =>
        TodaysMessageValidator.Validate(json, languages, [Compare], Pair, ["Ravi", "Asha"]);
}
