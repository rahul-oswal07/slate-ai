using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class CodeChecksTests
{
    private static readonly VerifierOptions Options = new();

    private static readonly ScriptContent Good = new(
        "ఈ రోజు 523 నుండి 168 ಕಳೆ చేద్దాం. ಕಳೆ అంటే తీసివేత. ఒక ದಶಕ తీసుకో, ದಶಕ విప్పు. ಬಿಡಿ పుల్లలు 13. 13 − 8 = 5. ಬಿಡಿ లెక్కపెట్టు. మీ టీచర్‌కు చూపించండి.",
        ["ಕಳೆ", "ದಶಕ", "ಬಿಡಿ"],
        "523 నుండి 168 తీసివేయి.",
        "పుల్లలు లేకపోతే రాళ్ళు వాడండి.",
        "ఏ బొమ్మలో 434 − 171 ఉంది?",
        "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ: 523 − 168 = 355. ನಿಮ್ಮ ಟೀಚರ್‌ಗೆ ತೋರಿಸು",
        "Subtraction with borrowing: 523 − 168 = 355. Show your teacher.",
        ["stones"]);

    private static readonly ActivityData Activity = new("M3.SUB.BORROW", "sub", "523 − 168", [523, 168], 355, null, null);
    private static readonly CheckData Check = new("M3.SUB.BORROW", "sub", "434 − 171", [363, 263, 273], 1, 263, null);

    private static VerifyInput Input(ScriptContent? content = null, CheckData? check = null, string language = "te") =>
        new(language, ["M3.SUB.BORROW"], content ?? Good, Activity, check ?? Check, ["Ravi", "Shobha"]);

    private static VerifierCheck Run(string name, VerifyInput input) =>
        CodeChecks.Run(input, A3ValidatorTests.Book, Options).Single(c => c.Name == name);

    [Fact]
    public void A_good_catch_up_passes_all_six_checks()
    {
        var checks = CodeChecks.Run(Input(), A3ValidatorTests.Book, Options);

        Assert.Equal(6, checks.Count);
        Assert.All(checks, c => Assert.True(c.Passed, $"{c.Name}: {c.Detail}"));
    }

    [Fact]
    public void Numbers_the_marked_option_must_be_the_recomputed_answer()
    {
        var check = Run(CodeChecks.Numbers, Input(check: Check with { CorrectIndex = 0 }));

        Assert.False(check.Passed);
        Assert.Equal("the marked answer is not 263", check.Detail);
    }

    [Fact]
    public void Numbers_only_one_option_may_be_correct()
    {
        Assert.Equal("more than one picture shows 263",
            Run(CodeChecks.Numbers, Input(check: Check with { Options = [263, 263, 273] })).Detail);
    }

    [Fact]
    public void Numbers_a_wrong_sum_in_the_script_is_caught_with_the_right_answer()
    {
        var check = Run(CodeChecks.Numbers, Input(Good with { Script = Good.Script.Replace("13 − 8 = 5", "13 − 8 = 6") }));

        Assert.False(check.Passed);
        Assert.Equal("says 13 − 8 = 6; it is 5", check.Detail);
    }

    [Fact]
    public void Numbers_the_script_must_use_the_activitys_numbers()
    {
        var check = Run(CodeChecks.Numbers, Input(Good with { Script = Good.Script.Replace("523", "500") }));

        Assert.Equal("the script doesn't use the activity's number 523", check.Detail);
    }

    [Fact]
    public void Numbers_the_check_must_test_a_target()
    {
        Assert.False(Run(CodeChecks.Numbers, Input(check: Check with { CompetencyId = "M3.ADD.CARRY" })).Passed);
    }

    [Theory]
    [InlineData("add", "456 + 278", 734)]
    [InlineData("sub", "523 − 168", 355)]
    [InlineData("expanded", "300 + 40 + 7", 347)]
    [InlineData("count", "936", 936)]
    [InlineData("placeValue", "4 in 347", 40)]
    [InlineData("biggest", "347 · 437 · 374", 437)]
    [InlineData("smallest", "347 · 437 · 374", 347)]
    public void Every_question_kind_is_recomputed(string kind, string question, int answer)
    {
        Assert.Equal(answer, CodeChecks.Recompute(kind, question));
    }

    [Fact]
    public void Script_mostly_in_another_alphabet_fails_with_the_share()
    {
        var latin = Good with { Script = "Today we subtract 523 minus 168 with ಕಳೆ ಕಳೆ ದಶಕ ದಶಕ ಬಿಡಿ ಬಿಡಿ, then show your teacher. రోజు" };

        var check = Run(CodeChecks.ScriptBlock, Input(latin));

        Assert.False(check.Passed);
        Assert.StartsWith("only ", check.Detail);
        Assert.Contains("Telugu letters", check.Detail);
    }

    [Fact]
    public void Length_too_many_words_fails_with_seconds_and_the_limit()
    {
        var words = string.Join(' ', Enumerable.Repeat("పదం", 187));
        var check = Run(CodeChecks.Length, Input(Good with { Script = Good.Script + " " + words }));

        Assert.False(check.Passed);
        Assert.Matches(@"^about \d+ seconds, limit 90$", check.Detail);
    }

    [Fact]
    public void Key_words_must_each_be_said_twice_and_come_from_the_lesson()
    {
        var once = Run(CodeChecks.KeyWords, Input(Good with { Script = Good.Script.Replace("ಬಿಡಿ లెక్కపెట్టు", "లెక్కపెట్టు") }));
        var foreign = Run(CodeChecks.KeyWords, Input(Good with { KeyWordsKn = ["ಕಳೆ", "ದಶಕ", "ನೂರು"] }));

        Assert.Equal("the key word ಬಿಡಿ is said 1 time(s), needs 2", once.Detail);
        Assert.False(foreign.Passed);
    }

    [Fact]
    public void Objects_must_be_named_and_allowed()
    {
        Assert.Equal("no household object suggested", Run(CodeChecks.Objects, Input(Good with { Objects = [] })).Detail);
        Assert.Equal("marbles is not on the allowed list", Run(CodeChecks.Objects, Input(Good with { Objects = ["marbles"] })).Detail);
    }

    [Fact]
    public void Names_and_placeholders_fail()
    {
        Assert.Equal("has a name from the class", Run(CodeChecks.NamesCheck, Input(Good with { GlossEn = "Ravi learns borrowing." })).Detail);
        Assert.Equal("has a placeholder or markup", Run(CodeChecks.NamesCheck, Input(Good with { CheckPrompt = "{child}, tap" })).Detail);
    }
}
