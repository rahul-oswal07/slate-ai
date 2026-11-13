using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

public sealed class ExerciseAnswersTests
{
    [Theory]
    [InlineData(48, 33, ">")]
    [InlineData(48, 84, "<")]
    [InlineData(86, 86, "=")]
    [InlineData(69, 71, "<")]
    [InlineData(99, 86, ">")]
    public void The_sign_between_two_numbers_is_computed(int a, int b, string sign) =>
        Assert.Equal(sign, ExerciseAnswers.CompareSign(a, b));

    [Fact]
    public void The_shipped_p_4_exercises_give_the_answers_in_curriculum_md()
    {
        // docs/design/curriculum.md "Nalikali demo lesson": Activity 2 signs and Activity 1 leaves.
        var lesson = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"))
            .Get("M3.BRIDGE.COMPARE")!;

        Assert.Equal(
            [">", "<", ">", "<", "=", "=", "<", "<", "=", ">"],
            lesson.Exercises!.Compare!.Select(p => ExerciseAnswers.CompareSign(p[0], p[1])));
        Assert.Equal(
            [(3, 64), (16, 46), (18, 82), (56, 81), (40, 90)],
            lesson.Exercises.Extremes!.Select(ExerciseAnswers.Extremes));
    }
}
