using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonAudio;
using SlateAi.Api.Tests.Features.CatchUps;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.LessonAudio;

public sealed class LessonScriptTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    /// <summary>Textbook example "89: face value 8, place value 80" and page 15 of part 2: the only numbers a script may say.</summary>
    internal static readonly Competency Lesson = new(
        "M3.PV.FACE", "ಮುಖಬೆಲೆ", "Face value and place value", [], null, 1, 1,
        ["89: face value 8, place value 80"], new TextbookPage("textbook", 2, 15),
        NameTe: "ముఖ విలువ", NameHi: "अंकों का मान", KeyWordsKn: ["ಮುಖಬೆಲೆ", "ಸ್ಥಾನಬೆಲೆ"]);

    private static string Json(string script) => $"{{\"script\":\"{script}\"}}";

    [Fact]
    public void A_valid_script_in_the_familys_script_with_only_textbook_numbers_is_accepted()
    {
        var result = LessonScriptValidator.Validate(
            Json("ఈ రోజు మేము ముఖ విలువ నేర్చుకున్నాం. 89 లో 8 ముఖ విలువ, 80 స్థాన విలువ. పుస్తకం 15 వ పేజీలో హోంవర్క్ చేసి మీ టీచర్‌కు చూపించండి."),
            "te", [Lesson], ["Ravi"]);

        Assert.Equal("valid", result.Verdict);
        Assert.NotNull(result.Script);
    }

    [Theory]
    [InlineData("{}", "fallback:invalid-output")]
    [InlineData("not json", "fallback:invalid-output")]
    [InlineData("{\"script\":\"  \"}", "fallback:invalid-output")]
    public void Missing_or_empty_output_is_refused(string json, string verdict) =>
        Assert.Equal(verdict, LessonScriptValidator.Validate(json, "te", [Lesson], []).Verdict);

    [Fact]
    public void A_childs_name_or_a_placeholder_is_refused()
    {
        Assert.Equal("fallback:name",
            LessonScriptValidator.Validate(Json("ఈ రోజు Ravi ముఖ విలువ నేర్చుకున్నాడు."), "te", [Lesson], ["Ravi"]).Verdict);
        Assert.Equal("fallback:placeholder",
            LessonScriptValidator.Validate(Json("ఈ రోజు {child} ముఖ విలువ నేర్చుకున్నాడు."), "te", [Lesson], ["Ravi"]).Verdict);
    }

    [Fact]
    public void A_number_that_is_not_in_the_lessons_examples_or_page_is_refused()
    {
        var result = LessonScriptValidator.Validate(
            Json("ఈ రోజు ముఖ విలువ నేర్చుకున్నాం. 57 లో 5 ముఖ విలువ."), "te", [Lesson], []);

        Assert.Equal("fallback:numbers", result.Verdict);
    }

    [Fact]
    public void The_wrong_script_or_a_script_that_is_too_long_is_refused()
    {
        Assert.Equal("fallback:script",
            LessonScriptValidator.Validate(Json("Today we learned face value and place value."), "te", [Lesson], []).Verdict);
        var long_ = string.Join(" ", Enumerable.Repeat("ముఖ", LessonScriptValidator.MaxWords + 1));
        Assert.Equal("fallback:too-long", LessonScriptValidator.Validate(Json(long_), "te", [Lesson], []).Verdict);
    }

    [Fact]
    public async Task Claude_is_asked_in_the_familys_language_and_gets_curriculum_data_only()
    {
        var aiCalls = new FakeAiCallStore();
        var llm = new ScriptWriterTests.StubLlm(aiCalls) { Json = Json("ఈ రోజు ముఖ విలువ. 89, 8, 80, 15.") };
        var writer = new LessonScriptWriter(
            llm, aiCalls,
            PromptLibrary.Load([("a8-lesson-recap.md", "---\nid: a8-lesson-recap\nversion: 1\nmodel_key: Script\n---\nPrompt.")], ["Script"]));

        await writer.WriteAsync("hi", [Lesson], null, ["Ravi", "Shobha"], Ct);

        var request = llm.Requests.Single();
        Assert.Equal(LessonScriptWriter.PromptId, request.Prompt.Id);
        Assert.Contains("\"languageName\":\"Hindi\"", request.UserContent);
        Assert.Contains("अंकों का मान", request.UserContent); // the lesson's name in the family's language
        Assert.Contains("89: face value 8", request.UserContent);
        Assert.DoesNotContain("Ravi", request.UserContent);
        Assert.DoesNotContain("Shobha", request.UserContent);
        Assert.DoesNotContain("teacherExamples", request.UserContent); // the clip is shared across classes
        Assert.Equal("fallback:script", Assert.Single(aiCalls.Calls).Verdict); // Telugu words for a Hindi family: refused and logged
    }
}
