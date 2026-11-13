namespace SlateAi.Api.Curriculum;

/// <summary>
/// Answers to the book's exercises (D-93), computed here and never by the AI (guardrail 4). Pure. Used by today's
/// message and the picture question (T-88, T-90); <see cref="Check"/> validates a curriculum file at startup.
/// </summary>
public static class ExerciseAnswers
{
    public const int Max = 999;

    /// <summary>"&gt;" if <paramref name="a"/> is more, "&lt;" if less, "=" if equal: the sign between a and b.</summary>
    public static string CompareSign(int a, int b) => a > b ? ">" : a < b ? "<" : "=";

    /// <summary>The smallest and the biggest number of a set.</summary>
    public static (int Smallest, int Biggest) Extremes(IReadOnlyList<int> numbers) => (numbers.Min(), numbers.Max());

    /// <summary>Problems with an exercise list; empty when it is usable.</summary>
    public static IEnumerable<string> Check(ExerciseList exercises)
    {
        var compare = exercises.Compare ?? [];
        var extremes = exercises.Extremes ?? [];
        if (compare.Count == 0 && extremes.Count == 0)
        {
            yield return "exercises can be left out but not empty";
        }

        if (compare.Concat(extremes).Any(item => item is null || item.Any(n => n is < 0 or > Max)))
        {
            yield return $"exercises: every number must be 0 to {Max}";
        }

        if (compare.Any(pair => pair is not { Count: 2 }))
        {
            yield return "exercises.compare: each item is a pair of 2 numbers";
        }

        if (extremes.Any(set => set is null || set.Count is < 2 or > 6 || set.Distinct().Count() != set.Count))
        {
            yield return "exercises.extremes: each item is 2 to 6 different numbers";
        }
    }
}
