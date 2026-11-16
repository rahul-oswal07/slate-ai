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

    /// <summary>Addition with carrying, with the book's add pairs (T-128); no examples, so the allowed numbers are the book's only.</summary>
    internal static readonly Competency Carry = new(
        "M3.ADD.CARRY", "ದಶಕ ಸಹಿತ ಸಂಕಲನ", "Addition with carrying", [], null, 3, 12, [], new TextbookPage("textbook", 1, 80),
        KeyWordsKn: ["ಕೂಡು", "ದಶಕ", "ಬಿಡಿ"],
        Practice: new PracticeRule("add", 3, Carry: "any"),
        Exercises: new ExerciseList(Add: [[248, 438], [466, 176], [456, 367]]));

    /// <summary>Subtraction with borrowing, with the book's sub pairs (T-128).</summary>
    internal static readonly Competency Borrow = new(
        "M3.SUB.BORROW", "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ", "Subtraction with borrowing", [], null, 4, 24, [], new TextbookPage("textbook", 1, 108),
        KeyWordsKn: ["ಕಳೆ", "ದಶಕ", "ಬಿಡಿ"],
        Practice: new PracticeRule("sub", 3, Borrow: "tens"),
        Exercises: new ExerciseList(Sub: [[872, 586], [290, 178], [847, 563]]));

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
    public void Only_a_lesson_with_the_books_exercises_gets_a_picture_question()
    {
        Assert.True(TodaysPicturePicker.Supported([Compare]));
        Assert.False(TodaysPicturePicker.Supported([NoExercises]));
        Assert.Equal(Pair, TodaysPicturePicker.Pick([NoExercises, Compare]));
    }

    [Fact]
    public void An_add_or_sub_lesson_gets_a_sum_picture_only_with_the_flag_on()
    {
        // D-116: today's message for sums is behind TodaysMessage:ArithmeticExercises (on by default since its eval run).
        Assert.False(TodaysPicturePicker.Supported([Carry]));
        Assert.False(TodaysPicturePicker.Supported([Borrow]));
        Assert.True(TodaysPicturePicker.Supported([Carry], arithmetic: true));
        Assert.True(TodaysPicturePicker.Supported([Borrow], arithmetic: true));
        Assert.False(TodaysPicturePicker.Supported([NoExercises], arithmetic: true));
    }

    [Fact]
    public void Sums_are_on_by_default_and_in_the_shipped_settings()
    {
        // D-116: a9-todays-message-sums v1 passed its eval run on 15 Nov, so staging and prod get sums without a setting.
        Assert.True(new TodaysMessageOptions().ArithmeticExercises);
        var settings = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "appsettings.json")));
        Assert.True(settings.RootElement.GetProperty("TodaysMessage").GetProperty("ArithmeticExercises").GetBoolean());
    }

    [Fact]
    public void A_sum_picture_is_the_books_pair_with_the_answer_and_two_usual_mistakes_by_code()
    {
        // 248 + 438 = 686; forgetting the carried ten gives 676. 872 − 586 = 286; smaller-from-bigger in each column, 314.
        Assert.Equal(("add", 248, 438, "686", "676 696 686"), Flat(TodaysPicturePicker.Pick([Carry], 0, arithmetic: true)));
        Assert.Equal(("sub", 872, 586, "286", "286 314 296"), Flat(TodaysPicturePicker.Pick([Borrow], 0, arithmetic: true)));
        Assert.Equal(("add", 466, 176, "642", "642 632 652"), Flat(TodaysPicturePicker.Pick([Carry], 1, arithmetic: true)));
        Assert.Equal(Flat(TodaysPicturePicker.Pick([Carry], 0, arithmetic: true)), Flat(TodaysPicturePicker.Pick([Carry], 3, arithmetic: true))); // wraps round
    }

    [Theory]
    [InlineData("add", 248, 438)]
    [InlineData("add", 466, 176)]
    [InlineData("add", 109, 37)]
    [InlineData("add", 348, 231)]
    [InlineData("sub", 872, 586)]
    [InlineData("sub", 290, 178)]
    [InlineData("sub", 456, 325)]
    [InlineData("sub", 468, 365)]
    public void The_three_choices_hold_the_answer_once_and_are_all_different_and_in_range(string kind, int a, int b)
    {
        var answer = kind == "add" ? a + b : a - b;

        var choices = ExerciseAnswers.Choices(kind, a, b);

        Assert.Equal(3, choices.Count);
        Assert.Single(choices, c => c == answer);
        Assert.All(choices, c => Assert.InRange(c, 0, ExerciseAnswers.Max));
        Assert.All(choices.Where(c => c != answer), c => Assert.True(Math.Abs(c - answer) >= 10));
        Assert.Equal(choices, ExerciseAnswers.Choices(kind, a, b)); // the same every time: a redraft doesn't reshuffle
    }

    [Fact]
    public void A_compare_lesson_keeps_its_compare_picture_with_the_flag_on()
    {
        Assert.Equal(Pair, TodaysPicturePicker.Pick([Compare], 0, arithmetic: true));
        Assert.Equal(Pair, TodaysPicturePicker.Pick([Carry, Compare], 0, arithmetic: true));
    }

    [Fact]
    public void Sums_are_written_with_their_own_prompt_and_compare_keeps_v4s()
    {
        var sum = TodaysPicturePicker.Pick([Carry], 0, arithmetic: true)!;

        Assert.Equal("a9-todays-message-sums", TodaysMessageWriter.PromptFor(sum));
        Assert.Equal("a9-todays-message", TodaysMessageWriter.PromptFor(Pair));
    }

    [Fact]
    public void A9_hears_the_kind_of_a_sum_but_never_its_answer_or_choices()
    {
        var sum = TodaysPicturePicker.Pick([Carry], 0, arithmetic: true)!;

        using var json = JsonDocument.Parse(TodaysMessageWriter.UserContent(["kn"], [Carry], sum, []));

        var question = json.RootElement.GetProperty("pictureQuestion");
        Assert.Equal(("add", 248, 438), (question.GetProperty("kind").GetString(), question.GetProperty("a").GetInt32(), question.GetProperty("b").GetInt32()));
        Assert.False(question.TryGetProperty("answer", out _));
        Assert.False(question.TryGetProperty("options", out _));
    }

    [Fact]
    public void For_a_sum_the_books_numbers_their_digits_and_a_hundred_are_allowed_but_not_the_answer()
    {
        var sum = TodaysPicturePicker.Pick([Carry], 0, arithmetic: true)!;

        var allowed = TodaysMessageValidator.AllowedNumbers([Carry], sum);

        Assert.Superset(new HashSet<string> { "248", "438", "466", "176", "2", "4", "8", "3", "24", "43", "10", "100" }, allowed);
        Assert.DoesNotContain("686", allowed);
        Assert.DoesNotContain("676", allowed);
    }

    [Fact]
    public void A_family_gets_the_sum_and_its_choices_never_the_answer()
    {
        var sections = Sections() with { Picture = TodaysPicturePicker.Pick([Carry], 0, arithmetic: true)! };

        var view = TodaysFamilyView.For(sections, "kn");

        Assert.Equal(
            new SlateAi.Api.Features.EveningMessages.ParentPicture(248, 438, Kind: "add", Options: [676, 696, 686]),
            view.Picture,
            new ParentPictureComparer());
    }

    private static (string, int, int, string, string) Flat(TodaysPicture? p) =>
        (p!.Kind, p.A, p.B, p.Answer, string.Join(' ', p.Options ?? []));

    private sealed class ParentPictureComparer : IEqualityComparer<SlateAi.Api.Features.EveningMessages.ParentPicture?>
    {
        public bool Equals(SlateAi.Api.Features.EveningMessages.ParentPicture? x, SlateAi.Api.Features.EveningMessages.ParentPicture? y) =>
            x is not null && y is not null && x.A == y.A && x.B == y.B && x.Kind == y.Kind && (x.Options ?? []).SequenceEqual(y.Options ?? []);

        public int GetHashCode(SlateAi.Api.Features.EveningMessages.ParentPicture? obj) => 0;
    }

    [Fact]
    public void A9_gets_no_add_or_sub_keys_for_a_compare_lesson()
    {
        // The A9 input for the demo lesson is unchanged by T-128, so prompt v4's evals still hold.
        var content = TodaysMessageWriter.UserContent(["kn"], [Compare], Pair, []);

        Assert.DoesNotContain("\"add\"", content);
        Assert.DoesNotContain("\"sub\"", content);
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
        Assert.Equal(new SlateAi.Api.Features.EveningMessages.ParentPicture(48, 33), view.Picture); // never the answer (T-90)
        Assert.Null(view.Video);
        Assert.Null(view.Note);
    }

    // T-139, D-121: every lesson gets today's message; without the book's exercises it has no picture question.
    [Fact]
    public void A_message_without_a_picture_question_promises_concept_and_activity_only_and_sends_no_picture()
    {
        var sections = NoPicture();

        Assert.Equal([TodaysSectionKind.Concept, TodaysSectionKind.Activity], TodaysSectionKind.PromisedFor(sections));
        Assert.Equal([TodaysSectionKind.Concept, TodaysSectionKind.Activity, TodaysSectionKind.Picture], TodaysSectionKind.PromisedFor(Sections()));
        Assert.Null(sections.WithRemoved(TodaysSectionKind.Picture, true));
        Assert.True(sections.WithRemoved(TodaysSectionKind.Concept, true)!.HasAnySection);
        Assert.False(sections.WithRemoved(TodaysSectionKind.Concept, true)!.WithRemoved(TodaysSectionKind.Activity, true)!.HasAnySection);
        Assert.Null(TodaysFamilyView.For(sections, "hi").Picture);
        Assert.Null(TodaysSections.Parse(sections.ToJson())!.Picture); // stored and read back without one
    }

    [Fact]
    public void Without_a_picture_only_the_lessons_own_numbers_page_and_part_are_allowed()
    {
        // NoExercises: example "456 + 278", textbook part 1 page 80. Nothing from a picture, not even 10 for a bundle.
        var allowed = TodaysMessageValidator.AllowedNumbers([NoExercises], null);

        Assert.Contains("456", allowed);
        Assert.Contains("278", allowed);
        Assert.Contains("80", allowed);
        Assert.DoesNotContain("10", allowed);
        Assert.DoesNotContain("734", allowed);
    }

    [Fact]
    public void A_lesson_without_a_picture_has_its_own_prompt_and_the_other_inputs_stay_as_evaluated()
    {
        Assert.Equal("a9-todays-message-lesson", TodaysMessageWriter.PromptFor(null));
        Assert.Equal(TodaysMessageWriter.PromptId, TodaysMessageWriter.PromptFor(Pair));
        Assert.Contains("\"pictureQuestion\":null", TodaysMessageWriter.UserContent(["kn"], [NoExercises], null, null));
        Assert.Contains("\"pictureQuestion\":{\"a\":48,\"b\":33}", TodaysMessageWriter.UserContent(["kn"], [Compare], Pair, null));
    }

    [Fact]
    public void The_last_section_cannot_be_removed_and_unknown_ones_are_refused()
    {
        var two = Sections().WithRemoved(TodaysSectionKind.Concept, true)!.WithRemoved(TodaysSectionKind.Activity, true)!;

        Assert.False(two.WithRemoved(TodaysSectionKind.Picture, true)!.HasAnySection);
        Assert.Null(two.WithRemoved(TodaysSectionKind.Video, true)); // no video: this lesson has no link (T-94)
        Assert.Null(two.WithRemoved("greeting", true));
    }

    [Fact]
    public void Sections_round_trip_as_json_and_a_generating_row_has_none()
    {
        var sections = Sections();

        Assert.Equivalent(sections, TodaysSections.Parse(sections.ToJson()));
        Assert.Null(TodaysSections.Parse("null"));
    }

    [Theory]
    [InlineData("ok", null)]
    [InlineData("name", "fallback:name")]
    [InlineData("number", "fallback:numbers")]
    [InlineData("long note", "fallback:too-long")]
    [InlineData("english", "fallback:script")]
    public void Her_kannada_goes_through_the_same_checks(string text, string? verdict)
    {
        var note = text switch
        {
            "ok" => "ನಾವು ಎಲೆಗಳ ಮೇಲೆ 48 ಮತ್ತು 33 ಬರೆದು ಹೋಲಿಸಿದೆವು.",
            "name" => "Ravi ಎಲೆಗಳ ಮೇಲೆ ಬರೆದನು.",
            "number" => "ನಾವು 73 ಬರೆದೆವು.",
            "long note" => "ಒಂದು. ಎರಡು. ಮೂರು.",
            "english" => "We wrote numbers on leaves and compared them.",
            _ => throw new ArgumentOutOfRangeException(nameof(text)),
        };
        var her = new TodaysText("ಮೊದಲು ಹತ್ತರ ಕಟ್ಟುಗಳನ್ನು ನೋಡಿ.", ["ಕಡ್ಡಿಗಳ ಕಟ್ಟು ಮಾಡಿ."], note);

        var result = TodaysMessageValidator.Check("kn", her, TodaysMessageValidator.AllowedNumbers([Compare], Pair), ["Ravi"]);

        Assert.Equal(verdict, result?.Verdict);
        Assert.DoesNotContain("Ravi", result?.Detail ?? "");
    }

    [Fact]
    public void Her_edit_changes_only_the_kannada_of_that_section_and_marks_it_hers()
    {
        var edited = Sections().WithKannada(TodaysSectionKind.Activity, null, ["ಹೊಸ ಹೆಜ್ಜೆ."])!;

        Assert.Equal(["ಹೊಸ ಹೆಜ್ಜೆ."], edited.Activity.Steps["kn"]);
        Assert.Equal(["एक"], edited.Activity.Steps["hi"]);
        Assert.True(edited.Teacher);
        Assert.Null(Sections().WithKannada(TodaysSectionKind.Picture, "48", null)); // numbers only by code
        Assert.Equal("ನನ್ನ ಟಿಪ್ಪಣಿ", Sections().WithKannada(TodaysSectionKind.Note, "ನನ್ನ ಟಿಪ್ಪಣಿ", null)!.Kannada.Note);
    }

    [Theory]
    [InlineData(null, null, "notAnswered", false)]
    [InlineData(">", null, "gotIt", false)]
    [InlineData("<", null, "needsHelp", false)]
    [InlineData("<", ">", "gotIt", true)] // the latest counts (D-108)
    [InlineData(">", "<", "needsHelp", true)]
    public void The_level_comes_from_the_latest_submit_by_code(string? first, string? second, string level, bool locked)
    {
        Assert.Equal(level, TodaysLevel.For(first, second, ">"));
        Assert.Equal(locked, TodaysLevel.Locked(first, second));
    }

    /// <summary>A message for a lesson without the book's exercises (T-139): no picture question.</summary>
    internal static TodaysSections NoPicture() => Sections() with { Picture = null };

    internal static TodaysSections Sections() => new(
        new TodaysConcept(
            new Dictionary<string, string> { ["kn"] = "ಕನ್ನಡ", ["hi"] = "हिंदी" },
            new Dictionary<string, Guid> { ["kn"] = Guid.NewGuid(), ["hi"] = Guid.NewGuid() }),
        new TodaysActivity(new Dictionary<string, IReadOnlyList<string>> { ["kn"] = ["ಒಂದು"], ["hi"] = ["एक"] }),
        Pair);

    private static TodaysWriteResult Validate(string json, IReadOnlyList<string> languages) =>
        TodaysMessageValidator.Validate(json, languages, [Compare], Pair, ["Ravi", "Asha"]);
}
