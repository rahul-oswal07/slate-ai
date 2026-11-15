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

    [Theory]
    [InlineData(248, 438, 686)]
    [InlineData(456, 367, 823)]
    [InlineData(109, 37, 146)]
    public void A_sum_is_computed(int a, int b, int sum) => Assert.Equal(sum, ExerciseAnswers.Sum(a, b));

    [Theory]
    [InlineData(872, 586, 286)]
    [InlineData(290, 178, 112)]
    [InlineData(468, 365, 103)]
    public void A_difference_is_computed(int a, int b, int difference) =>
        Assert.Equal(difference, ExerciseAnswers.Difference(a, b));

    [Fact]
    public void The_shipped_add_and_sub_exercises_give_the_answers_in_the_lessons_examples()
    {
        // Each pair is copied from the lesson's own examples (T-128), so its answer is the one the book prints there.
        var book = CurriculumLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "data", "curriculum"));

        Assert.Equal([686, 642, 823], book.Get("M3.ADD.CARRY")!.Exercises!.Add!.Select(p => ExerciseAnswers.Sum(p[0], p[1])));
        Assert.Equal([579, 576, 998], book.Get("M3.ADD.NOCARRY")!.Exercises!.Add!.Select(p => ExerciseAnswers.Sum(p[0], p[1])));
        Assert.Equal([146, 387], book.Get("M3.ADD.WORD")!.Exercises!.Add!.Select(p => ExerciseAnswers.Sum(p[0], p[1])));
        Assert.Equal(
            [286, 112, 284], book.Get("M3.SUB.BORROW")!.Exercises!.Sub!.Select(p => ExerciseAnswers.Difference(p[0], p[1])));
        Assert.Equal(
            [131, 123, 121], book.Get("M3.SUB.NOBORROW")!.Exercises!.Sub!.Select(p => ExerciseAnswers.Difference(p[0], p[1])));
        Assert.Equal([103, 116], book.Get("M3.SUB.WORD")!.Exercises!.Sub!.Select(p => ExerciseAnswers.Difference(p[0], p[1])));
        Assert.Equal(
            [(157, 475), (524, 599)], book.Get("M3.NUM.COMPARE")!.Exercises!.Extremes!.Select(ExerciseAnswers.Extremes));
    }
}
