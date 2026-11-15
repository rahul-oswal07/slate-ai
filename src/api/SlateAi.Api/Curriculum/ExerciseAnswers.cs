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

    /// <summary>The sum of an "add" pair.</summary>
    public static int Sum(int a, int b) => a + b;

    /// <summary>The difference of a "sub" pair: the first number minus the second.</summary>
    public static int Difference(int a, int b) => a - b;

    /// <summary>Problems with an exercise list; empty when it is usable.</summary>
    public static IEnumerable<string> Check(ExerciseList exercises)
    {
        var compare = exercises.Compare ?? [];
        var extremes = exercises.Extremes ?? [];
        var add = exercises.Add ?? [];
        var sub = exercises.Sub ?? [];
        if (!exercises.HasAny)
        {
            yield return "exercises can be left out but not empty";
        }

        if (compare.Concat(extremes).Concat(add).Concat(sub).Any(item => item is null || item.Any(n => n is < 0 or > Max)))
        {
            yield return $"exercises: every number must be 0 to {Max}";
        }

        if (compare.Any(pair => pair is not { Count: 2 }))
        {
            yield return "exercises.compare: each item is a pair of 2 numbers";
        }

        // The book is the source of truth, so a pair isn't held to the lesson's practice rule (p 108's 872 − 586 borrows
        // twice): only what code needs to compute a whole-number answer within the book's range.
        if (add.Any(pair => pair is not { Count: 2 }))
        {
            yield return "exercises.add: each item is a pair of 2 numbers";
        }
        else if (add.Any(pair => Sum(pair[0], pair[1]) > Max))
        {
            yield return $"exercises.add: each sum must be {Max} or less";
        }

        if (sub.Any(pair => pair is not { Count: 2 }))
        {
            yield return "exercises.sub: each item is a pair of 2 numbers";
        }
        else if (sub.Any(pair => pair[0] < pair[1]))
        {
            yield return "exercises.sub: the first number must not be smaller than the second";
        }

        if (extremes.Any(set => set is null || set.Count is < 2 or > 6 || set.Distinct().Count() != set.Count))
        {
            yield return "exercises.extremes: each item is 2 to 6 different numbers";
        }
    }
}
