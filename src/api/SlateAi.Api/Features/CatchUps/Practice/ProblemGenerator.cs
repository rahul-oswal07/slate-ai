using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.CatchUps.Practice;

/// <summary>
/// A sum or question made by code (guardrail 4), with three options drawn as stick bundles (D-07).
/// </summary>
/// <param name="Kind">The lesson's practice kind, so A3 can word the question.</param>
/// <param name="Question">Language-neutral display text, e.g. "43 − 18", "300 + 40 + 7", "4 in 347".</param>
/// <param name="Options">Three numbers, shuffled; exactly one is <see cref="Answer"/>.</param>
/// <param name="CorrectIndex">Where the answer is in <see cref="Options"/>. Never sent to a parent's browser (P3).</param>
/// <param name="Numbers">The numbers in the question, e.g. [43, 18]; the activity picture starts from the first.</param>
public sealed record Problem(
    string Kind, string Question, int Answer, IReadOnlyList<int> Options, int CorrectIndex, IReadOnlyList<int> Numbers);

/// <summary>
/// Makes a lesson's sums and questions from its <see cref="PracticeRule"/>. Pure: the same <see cref="Random"/> seed
/// gives the same problem. Textbook limits: sums and minuends ≤ 999, no borrowing across a zero, distractors at
/// least 10 apart so they never differ by one loose stick (docs/design/curriculum.md, P3 C-09).
/// </summary>
public static class ProblemGenerator
{
    /// <summary>Options are at least this far apart (one bundle of ten).</summary>
    public const int MinGap = 10;

    public static Problem Make(PracticeRule rule, Random rng) => rule.Kind switch
    {
        "count" => Count(rule.Digits, rng, expanded: false),
        "expanded" => Count(rule.Digits, rng, expanded: true),
        "placeValue" => PlaceValue(rng),
        "biggest" => Extreme(rng, biggest: true),
        "smallest" => Extreme(rng, biggest: false),
        "add" => Add(rule.Digits, rule.Carry ?? "none", rng),
        "sub" => Sub(rule.Digits, rule.Borrow ?? "none", rng),
        _ => throw new ArgumentException($"Unknown practice kind '{rule.Kind}'.", nameof(rule)),
    };

    private static Problem Count(int digits, Random rng, bool expanded)
    {
        var (min, max) = Range(digits);
        int n;
        do
        {
            n = rng.Next(min, max + 1);
        }
        while (!DistinctNonZeroDigits(n));

        var question = expanded ? Expanded(n) : n.ToString();
        return Finish(expanded ? "expanded" : "count", question, n, [n], rng, min, max, Swaps(n).Concat([n + 100, n - 100, n + 10, n - 10]));
    }

    private static Problem PlaceValue(Random rng)
    {
        int n;
        do
        {
            n = rng.Next(100, 1000);
        }
        while (!DistinctNonZeroDigits(n) || n / 100 < 2 || n / 10 % 10 < 2);

        // The tens or hundreds digit (the ones digit's place value equals its face value).
        var place = rng.Next(2) == 0 ? 10 : 100;
        var digit = n / place % 10;
        return Finish("placeValue", $"{digit} in {n}", digit * place, [n], rng, 1, 999, [digit, digit * 10, digit * 100]);
    }

    private static Problem Extreme(Random rng, bool biggest)
    {
        // Same hundreds digit or swapped digits, so the child must compare place by place.
        int n;
        do
        {
            n = rng.Next(100, 1000);
        }
        while (!DistinctNonZeroDigits(n));

        var others = Swaps(n).Where(x => x is >= 100 and <= 999 && Math.Abs(x - n) >= MinGap).Distinct().ToList();
        var pool = new[] { n }.Concat(others).Distinct().ToList();
        var three = PickSpread(pool, 3, rng);
        var answer = biggest ? three.Max() : three.Min();
        var options = Shuffle(three, rng);
        return new Problem(
            biggest ? "biggest" : "smallest", string.Join(" · ", options), answer, options, IndexOf(options, answer), options);
    }

    private static Problem Add(int digits, string carry, Random rng)
    {
        var (min, max) = Range(digits);
        while (true)
        {
            var a = rng.Next(min, max + 1);
            var b = rng.Next(min, max + 1);
            var sum = a + b;
            var onesCarry = a % 10 + b % 10 >= 10;
            var tensCarry = a / 10 % 10 + b / 10 % 10 + (onesCarry ? 1 : 0) >= 10;
            var ok = sum <= max && carry switch
            {
                "none" => !onesCarry && !tensCarry,
                "ones" => onesCarry && !tensCarry,
                _ => onesCarry || tensCarry,
            };
            if (!ok)
            {
                continue;
            }

            // Forgetting the carried ten is the common mistake.
            var mistakes = new[] { onesCarry ? sum - 10 : sum + 10, sum + 10, sum - 10, sum + 100, sum - 100 };
            return Finish("add", $"{a} + {b}", sum, [a, b], rng, 1, max, mistakes);
        }
    }

    private static Problem Sub(int digits, string borrow, Random rng)
    {
        var (min, max) = Range(digits);
        while (true)
        {
            var a = rng.Next(min, max + 1);
            var b = rng.Next(min, a);
            var difference = a - b;
            if (difference < MinGap)
            {
                continue;
            }

            var (aOnes, aTens, aHundreds) = (a % 10, a / 10 % 10, a / 100);
            var (bOnes, bTens, bHundreds) = (b % 10, b / 10 % 10, b / 100);
            var ok = borrow switch
            {
                "none" => aOnes >= bOnes && aTens >= bTens && aHundreds >= bHundreds,
                // One borrow from the tens, never across a zero (the book doesn't teach that).
                _ => aOnes < bOnes && aTens >= 1 && aTens - 1 >= bTens && aHundreds >= bHundreds,
            };
            if (!ok)
            {
                continue;
            }

            // "Smaller from bigger" in each column is the common borrowing mistake.
            var columnWise = Math.Abs(aHundreds - bHundreds) * 100 + Math.Abs(aTens - bTens) * 10 + Math.Abs(aOnes - bOnes);
            var mistakes = new[] { columnWise, difference + 10, difference - 10, difference + 100, difference - 100 };
            return Finish("sub", $"{a} − {b}", difference, [a, b], rng, 1, max, mistakes);
        }
    }

    /// <summary>The answer plus two distractors from <paramref name="candidates"/> (then wider steps), all ≥ 10 apart.</summary>
    private static Problem Finish(
        string kind, string question, int answer, IReadOnlyList<int> numbers, Random rng, int min, int max, IEnumerable<int> candidates)
    {
        var fallbacks = new[] { 20, 30, 40, 50, 200, 300 }.SelectMany(step => new[] { answer + step, answer - step });
        var chosen = new List<int> { answer };
        foreach (var x in Shuffle(candidates.ToList(), rng).Concat(fallbacks))
        {
            if (chosen.Count == 3)
            {
                break;
            }

            if (x >= min && x <= max && chosen.All(c => Math.Abs(c - x) >= MinGap))
            {
                chosen.Add(x);
            }
        }

        var options = Shuffle(chosen, rng);
        return new Problem(kind, question, answer, options, IndexOf(options, answer), numbers);
    }

    private static List<int> PickSpread(IReadOnlyList<int> pool, int count, Random rng)
    {
        var chosen = new List<int> { pool[0] };
        foreach (var x in Shuffle(pool.Skip(1).ToList(), rng).Concat([pool[0] + 100, pool[0] - 100, pool[0] + 200]))
        {
            if (chosen.Count == count)
            {
                break;
            }

            if (x is >= 100 and <= 999 && chosen.All(c => Math.Abs(c - x) >= MinGap))
            {
                chosen.Add(x);
            }
        }

        return chosen;
    }

    /// <summary>The number with two of its digits swapped: what a child who mixes up places would pick.</summary>
    private static IEnumerable<int> Swaps(int n)
    {
        var digits = n.ToString().ToCharArray();
        for (var i = 0; i < digits.Length; i++)
        {
            for (var j = i + 1; j < digits.Length; j++)
            {
                var swapped = (char[])digits.Clone();
                (swapped[i], swapped[j]) = (swapped[j], swapped[i]);
                if (swapped[0] != '0')
                {
                    yield return int.Parse(new string(swapped));
                }
            }
        }
    }

    private static string Expanded(int n)
    {
        var digits = n.ToString();
        return string.Join(" + ", digits.Select((d, i) => (d - '0') * (int)Math.Pow(10, digits.Length - 1 - i)).Where(v => v > 0));
    }

    private static bool DistinctNonZeroDigits(int n) =>
        n.ToString() is var s && !s.Contains('0') && s.Distinct().Count() == s.Length;

    private static (int Min, int Max) Range(int digits) => digits switch
    {
        2 => (10, 99),
        3 => (100, 999),
        _ => (1000, 9999),
    };

    private static List<int> Shuffle(IReadOnlyList<int> items, Random rng)
    {
        var list = items.ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private static int IndexOf(IReadOnlyList<int> options, int answer) => options.ToList().IndexOf(answer);
}
