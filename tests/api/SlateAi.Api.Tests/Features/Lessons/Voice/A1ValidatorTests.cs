using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Lessons.Voice;

namespace SlateAi.Api.Tests.Features.Lessons.Voice;

public sealed class A1ValidatorTests
{
    private static readonly CurriculumGraph Book = new(
        new[] { "M3.ADD.CARRY", "M3.SUB.NOBORROW", "M3.SUB.CHECK", "M3.SUB.BORROW" }
            .Select((id, i) => new Competency(id, "ಕ", id, [], null, 5, i, [], null)));

    [Fact]
    public void Confident_valid_ids_are_mapped()
    {
        var result = A1Validator.Validate(
            """{"competencyIds":["M3.SUB.NOBORROW","M3.SUB.CHECK"],"confidence":0.86,"summaryKn":"ಕಳೆಯುವಿಕೆ","summaryEn":"Subtraction","examplesUsed":["452 - 131"]}""",
            Book);

        Assert.Equal("mapped", result.Status);
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.CHECK"], result.CompetencyIds);
        Assert.Equal("Subtraction", result.SummaryEn);
        Assert.Equal(["452 - 131"], result.ExamplesUsed);
        Assert.Equal("valid", result.Verdict);
    }

    [Fact]
    public void Below_0_7_is_low_confidence() =>
        Assert.Equal("low", A1Validator.Validate(
            """{"competencyIds":["M3.ADD.CARRY"],"confidence":0.69,"summaryKn":"","summaryEn":"","examplesUsed":[]}""", Book).Status);

    [Fact]
    public void Unknown_and_duplicate_ids_are_dropped_and_at_most_three_kept()
    {
        var result = A1Validator.Validate(
            """{"competencyIds":["M9.FAKE","M3.SUB.CHECK","M3.SUB.CHECK","M3.ADD.CARRY","M3.SUB.BORROW","M3.SUB.NOBORROW"],"confidence":0.9,"summaryKn":"","summaryEn":"","examplesUsed":[]}""",
            Book);

        Assert.Equal(["M3.SUB.CHECK", "M3.ADD.CARRY", "M3.SUB.BORROW"], result.CompetencyIds);
    }

    [Theory]
    [InlineData("""{"competencyIds":["M9.FAKE"],"confidence":0.95,"summaryKn":"","summaryEn":"","examplesUsed":[]}""", "fallback:no-valid-id")]
    [InlineData("""{"competencyIds":[],"confidence":0.95,"summaryKn":"","summaryEn":"","examplesUsed":[]}""", "fallback:no-valid-id")]
    [InlineData("not json", "fallback:invalid-output")]
    public void No_usable_id_is_unclear(string json, string verdict)
    {
        var result = A1Validator.Validate(json, Book);

        Assert.Equal("unclear", result.Status);
        Assert.Empty(result.CompetencyIds);
        Assert.Equal(verdict, result.Verdict);
    }

    [Fact]
    public void A_confidence_outside_0_to_1_counts_as_low() =>
        Assert.Equal("low", A1Validator.Validate(
            """{"competencyIds":["M3.ADD.CARRY"],"confidence":7,"summaryKn":"","summaryEn":"","examplesUsed":[]}""", Book).Status);
}
