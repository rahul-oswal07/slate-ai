using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Features.Lessons;

/// <summary>The eval case saved when she confirms over A1's match (T-102).</summary>
public sealed class LessonFeedbackBuilderTests
{
    private static readonly DateOnly Day = new(2026, 11, 27);
    private static readonly DateTimeOffset StartedAt = new(2026, 11, 27, 5, 30, 0, TimeSpan.Zero);

    private static LessonRecord Match(string status, params string[] ids) =>
        new(ids, LessonValues.Voice, status, "ಕಳೆಯುವಿಕೆ", "Subtraction, checked by adding", ["865 - 432"], StartedAt, 0.86);

    [Fact]
    public void The_saved_case_carries_the_match_her_choice_and_A1s_own_words()
    {
        var feedback = LessonFeedbackBuilder.From(
            Day, Match(LessonValues.Mapped, "M3.SUB.NOBORROW", "M3.SUB.MAKE"), ["M3.SUB.NOBORROW"], LessonValues.Voice, 3)!;

        Assert.Equal(Day, feedback.Date);
        Assert.Equal(StartedAt, feedback.MappingStartedAt);
        Assert.Equal(3, feedback.PromptVersion);
        Assert.Equal(LessonValues.Mapped, feedback.MatchStatus);
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.MAKE"], feedback.MatchedCompetencyIds);
        Assert.Equal(0.86, feedback.Confidence);
        Assert.Equal(["M3.SUB.NOBORROW"], feedback.ChosenCompetencyIds);
        Assert.Equal(LessonValues.Voice, feedback.ConfirmedVia);
        Assert.Equal(LessonFeedbackBuilder.Edited, feedback.Outcome);
        Assert.Equal("ಕಳೆಯುವಿಕೆ", feedback.SummaryKn);
        Assert.Equal("Subtraction, checked by adding", feedback.SummaryEn);
        Assert.Equal(["865 - 432"], feedback.ExamplesUsed);
    }

    [Fact]
    public void The_case_has_no_field_for_a_name_a_transcript_or_audio()
    {
        // Guardrail 1 and the Lessons privacy rule: only IDs and A1's (name-scrubbed) wording are kept.
        var fields = typeof(LessonFeedbackRecord).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain(fields, f => f.Contains("Transcript", StringComparison.OrdinalIgnoreCase)
            || f.Contains("Audio", StringComparison.OrdinalIgnoreCase)
            || f.Contains("Name", StringComparison.OrdinalIgnoreCase)
            || f.Contains("Child", StringComparison.OrdinalIgnoreCase)
            || f.Contains("Teacher", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Nothing_is_saved_for_a_day_with_no_lesson_yet()
    {
        Assert.Null(LessonFeedbackBuilder.From(Day, null, ["M3.SUB.MAKE"], LessonValues.Picked, 3));
    }

    public static TheoryData<LessonRecord> NoMatch => new()
    {
        new LessonRecord(["M3.SUB.MAKE"], LessonValues.Picked, LessonValues.Confirmed),
        new LessonRecord(["M3.SUB.MAKE"], LessonValues.Voice, LessonValues.Confirmed, "ಕ", "x", [], StartedAt, 0.9),
        new LessonRecord([], LessonValues.Voice, LessonValues.Processing, MappingStartedAt: StartedAt),
        new LessonRecord([], LessonValues.Voice, LessonValues.Unclear, MappingStartedAt: StartedAt),
        new LessonRecord(["M3.SUB.MAKE"], LessonValues.Voice, LessonValues.Mapped),
    };

    [Theory]
    [MemberData(nameof(NoMatch))]
    public void Nothing_is_saved_without_an_A1_match_to_compare_with(LessonRecord current)
    {
        Assert.Null(LessonFeedbackBuilder.From(Day, current, ["M3.SUB.MAKE"], LessonValues.Picked, 3));
    }

    [Theory]
    // Mapped: every matched lesson was ticked.
    [InlineData("mapped", "A,B", "A,B", "accepted")]
    [InlineData("mapped", "A,B", "B,A", "accepted")]
    [InlineData("mapped", "A,B", "A", "edited")]
    [InlineData("mapped", "A", "A,C", "edited")]
    [InlineData("mapped", "A,B", "C", "replaced")]
    // Low: she chose between the first two.
    [InlineData("low", "A,B,C", "B", "accepted")]
    [InlineData("low", "A,B,C", "A", "accepted")]
    [InlineData("low", "A,B,C", "C", "replaced")]
    [InlineData("low", "A,B", "A,D", "edited")]
    [InlineData("low", "A,B", "D", "replaced")]
    public void Outcome_compares_her_choice_with_what_T2_offered(string status, string matched, string chosen, string outcome)
    {
        Assert.Equal(outcome, LessonFeedbackBuilder.Outcome(status, matched.Split(','), chosen.Split(',')));
    }
}
