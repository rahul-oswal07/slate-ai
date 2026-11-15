using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Features.TodaysMessage;

/// <summary>What of her lesson may reach A9 (T-120): pure code, guardrails 1 and 4.</summary>
public sealed class TodaysFromClassTests
{
    private static readonly DateOnly Today = new(2026, 11, 16);
    private static readonly IReadOnlyList<string> Lessons = [TodaysMessageTests.Compare.Id];
    private static readonly IReadOnlySet<string> Allowed = TodaysMessageValidator.AllowedNumbers(
        [TodaysMessageTests.Compare], TodaysPicturePicker.Pick([TodaysMessageTests.Compare])!);

    private static TodaysFromClass? From(ConfirmedLesson? lesson) => TodaysFromClassFilter.From(lesson, Today, Lessons, Allowed, ["Manjunath", "Rohan"]);

    private static ConfirmedLesson Lesson(string? summary, params string[] examples) => new(Guid.NewGuid(), Today, Lessons, summary, examples);

    [Fact]
    public void Keeps_her_summary_and_examples_with_the_books_numbers()
    {
        var result = From(Lesson(" ಬಳೆಗಳಿಂದ ಹೋಲಿಸಿದೆವು. ", "bangles", "48 and 33", "bangles"));

        Assert.Equal("ಬಳೆಗಳಿಂದ ಹೋಲಿಸಿದೆವು.", result!.SummaryKn);
        Assert.Equal(["bangles", "48 and 33"], result.Examples);
    }

    [Theory]
    [InlineData("Rohan helped")] // a child's name (guardrail 1)
    [InlineData("rohan's sticks")]
    [InlineData("{teacher} showed it")] // a placeholder
    [InlineData("48 > 33")] // a sign the family page asks for later
    [InlineData("27 + 15")] // numbers not in the book's list (guardrail 4)
    public void Drops_a_phrase_that_could_leak_a_name_or_a_number(string example)
    {
        Assert.Null(From(Lesson(null, example)));
    }

    [Fact]
    public void Drops_a_summary_with_a_number_off_the_book_but_keeps_safe_examples()
    {
        var result = From(Lesson("ನಾವು 27 ಮತ್ತು 15 ಕೂಡಿಸಿದೆವು.", "bundles of sticks"));

        Assert.Null(result!.SummaryKn);
        Assert.Equal(["bundles of sticks"], result.Examples);
    }

    [Fact]
    public void Says_nothing_for_another_day_another_lesson_or_no_lesson()
    {
        Assert.Null(From(null));
        Assert.Null(From(Lesson("ಬಳೆಗಳು", "bangles") with { Date = Today.AddDays(-1) }));
        Assert.Null(From(Lesson("ಬಳೆಗಳು", "bangles") with { CompetencyIds = ["M3.ADD.CARRY"] }));
        Assert.Null(From(Lesson(null)));
    }

    [Fact]
    public void Keeps_at_most_five_short_examples()
    {
        var result = From(Lesson(null, new string('x', TodaysFromClassFilter.MaxChars + 1), "a", "b", "c", "d", "e", "f"));

        Assert.Equal(["a", "b", "c", "d", "e"], result!.Examples);
    }
}
