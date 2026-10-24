using SlateAi.Api.Features.CatchUps.Compression;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class A2ValidatorTests
{
    private static readonly CompressionInput Input = new(
        Guid.NewGuid(),
        [Missed("M3.SUB.NOBORROW"), Missed("M3.SUB.CHECK"), Missed("M3.SUB.BORROW")],
        ["M3.SUB.WORD"],
        ["M3.SUB.BORROW", "M3.SUB.NOBORROW"]);

    [Fact]
    public void Valid_targets_keep_their_order_and_reasons_and_every_other_missed_lesson_is_dropped()
    {
        var result = A2Validator.Validate(
            """
            {"targets":[{"competencyId":"M3.SUB.BORROW","reason":"Word problems next week need borrowing."}],
             "dropped":[{"competencyId":"M3.SUB.CHECK","reason":"Checking can wait."}]}
            """, Input);

        Assert.Equal(CompressionSource.Ai, result.Source);
        Assert.Equal("valid", result.Verdict);
        Assert.Equal([new CompressionItem("M3.SUB.BORROW", "Word problems next week need borrowing.")], result.Targets);
        Assert.Equal(
            [new CompressionItem("M3.SUB.NOBORROW", null), new CompressionItem("M3.SUB.CHECK", "Checking can wait.")],
            result.Dropped);
    }

    [Fact]
    public void A_kannada_reason_is_kept_next_to_the_english_one()
    {
        var result = A2Validator.Validate(
            """
            {"targets":[{"competencyId":"M3.SUB.BORROW","reason":"Word problems next week need borrowing.","reasonKn":"ಮುಂದಿನ ವಾರದ ಪದ ಸಮಸ್ಯೆಗಳಿಗೆ 3 ಅಂಕಿಯ ಎರವಲು ವ್ಯವಕಲನ ಬೇಕು."}],
             "dropped":[{"competencyId":"M3.SUB.CHECK","reason":"Checking can wait.","reasonKn":"ಪರಿಶೀಲನೆ ಕಾಯಬಹುದು."}]}
            """, Input);

        Assert.Equal("valid", result.Verdict);
        Assert.Equal("ಮುಂದಿನ ವಾರದ ಪದ ಸಮಸ್ಯೆಗಳಿಗೆ 3 ಅಂಕಿಯ ಎರವಲು ವ್ಯವಕಲನ ಬೇಕು.", result.Targets[0].ReasonKn);
        Assert.Equal("Word problems next week need borrowing.", result.Targets[0].Reason);
        Assert.Equal("ಪರಿಶೀಲನೆ ಕಾಯಬಹುದು.", result.Dropped.Single(d => d.Id == "M3.SUB.CHECK").ReasonKn);
    }

    [Theory]
    [InlineData("\"reasonKn\":\"Word problems need borrowing.\"")] // English in the Kannada field
    [InlineData("\"reasonKn\":\"ಪದ problems next week need borrowing.\"")] // mostly English
    [InlineData("\"reasonKn\":\"   \"")]
    [InlineData("\"reasonKn\":null")]
    [InlineData("\"other\":1")] // no reasonKn at all, as v1 answered
    public void A_kannada_reason_that_is_not_kannada_is_dropped_and_the_targets_still_stand(string field)
    {
        var result = A2Validator.Validate(
            $$"""
            {"targets":[{"competencyId":"M3.SUB.BORROW","reason":"Word problems need borrowing.",{{field}}}],"dropped":[]}
            """, Input);

        Assert.Equal(CompressionSource.Ai, result.Source);
        Assert.Equal("M3.SUB.BORROW", result.Targets.Single().Id);
        Assert.Equal("Word problems need borrowing.", result.Targets.Single().Reason);
        Assert.Null(result.Targets.Single().ReasonKn);
    }

    [Fact]
    public void An_assignment_stored_before_kannada_reasons_still_reads()
    {
        var item = System.Text.Json.JsonSerializer.Deserialize<CompressionItem>(
            """{"id":"M3.SUB.BORROW","reason":"Needed."}""", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Equal(new CompressionItem("M3.SUB.BORROW", "Needed."), item);
        Assert.Null(item!.ReasonKn);
    }

    [Theory]
    [InlineData("not json", "fallback:invalid-output")]
    [InlineData("""{"dropped":[]}""", "fallback:invalid-output")]
    [InlineData("""{"targets":[],"dropped":[]}""", "fallback:target-count")]
    [InlineData("""{"targets":[{"competencyId":"M3.SUB.BORROW","reason":"a"},{"competencyId":"M3.SUB.NOBORROW","reason":"b"},{"competencyId":"M3.SUB.CHECK","reason":"c"}],"dropped":[]}""", "fallback:target-count")]
    [InlineData("""{"targets":[{"competencyId":"M3.SUB.CHECK","reason":"not a candidate"}],"dropped":[]}""", "fallback:target-outside-candidates")]
    [InlineData("""{"targets":[{"competencyId":"M9.FAKE","reason":"invented"}],"dropped":[]}""", "fallback:target-outside-candidates")]
    [InlineData("""{"targets":[{"competencyId":"M3.SUB.BORROW","reason":"a"},{"competencyId":"M3.SUB.BORROW","reason":"b"}],"dropped":[]}""", "fallback:target-outside-candidates")]
    [InlineData("""{"targets":[{"reason":"no id"}],"dropped":[]}""", "fallback:target-outside-candidates")]
    public void Anything_invalid_falls_back_to_the_nearest_prerequisite_with_no_ai_reasons(string json, string verdict)
    {
        var result = A2Validator.Validate(json, Input);

        Assert.Equal(CompressionSource.Rule, result.Source);
        Assert.Equal(verdict, result.Verdict);
        Assert.Equal([new CompressionItem("M3.SUB.BORROW", null)], result.Targets);
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.CHECK"], result.Dropped.Select(d => d.Id));
        Assert.All(result.Dropped, d => Assert.Null(d.Reason));
    }

    [Fact]
    public void Dropped_lessons_that_were_not_missed_are_ignored_and_long_reasons_are_cut()
    {
        var result = A2Validator.Validate(
            $$"""
            {"targets":[{"competencyId":"M3.SUB.BORROW","reason":"{{new string('x', 300)}}"},{"competencyId":"M3.SUB.NOBORROW","reason":"  needed  "}],
             "dropped":[{"competencyId":"M3.ADD.CARRY","reason":"not missed"}]}
            """, Input);

        Assert.Equal(A2Validator.MaxReasonLength, result.Targets[0].Reason!.Length);
        Assert.Equal("needed", result.Targets[1].Reason);
        Assert.Equal([new CompressionItem("M3.SUB.CHECK", null)], result.Dropped);
    }

    [Fact]
    public void With_no_candidates_the_rule_takes_the_latest_missed_lesson()
    {
        var input = Input with { Candidates = [] };

        var result = A2Validator.Rule(input, "rule:no-candidates");

        Assert.Equal([new CompressionItem("M3.SUB.BORROW", null)], result.Targets);
        Assert.Equal(CompressionSource.Rule, result.Source);
    }

    [Fact]
    public void Schema_limits_targets_to_the_candidates_and_dropped_to_the_missed_lessons()
    {
        var schema = A2Validator.Schema(Input);

        string[] Enum(string field) => schema.GetProperty("properties").GetProperty(field).GetProperty("items")
            .GetProperty("properties").GetProperty("competencyId").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();

        Assert.Equal(["M3.SUB.BORROW", "M3.SUB.NOBORROW"], Enum("targets"));
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.CHECK", "M3.SUB.BORROW"], Enum("dropped"));
    }

    [Fact]
    public void Schema_asks_for_a_reason_in_english_and_in_kannada()
    {
        var items = A2Validator.Schema(Input).GetProperty("properties").GetProperty("targets").GetProperty("items");

        Assert.Equal(["competencyId", "reason", "reasonKn"], items.GetProperty("required").EnumerateArray().Select(e => e.GetString()!));
        Assert.True(items.GetProperty("properties").TryGetProperty("reasonKn", out _));
    }

    private static MissedCompetency Missed(string id) => new(id, [new DateOnly(2026, 10, 20)]);
}
