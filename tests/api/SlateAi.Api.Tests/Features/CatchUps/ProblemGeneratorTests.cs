using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Practice;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class ProblemGeneratorTests
{
    private const int Seeds = 300;

    public static TheoryData<string, int, string?, string?> Rules => new()
    {
        { "count", 2, null, null },
        { "count", 3, null, null },
        { "count", 4, null, null },
        { "expanded", 3, null, null },
        { "placeValue", 3, null, null },
        { "biggest", 3, null, null },
        { "smallest", 3, null, null },
        { "add", 2, "ones", null },
        { "add", 3, "none", null },
        { "add", 3, "ones", null },
        { "add", 3, "any", null },
        { "sub", 2, null, "tens" },
        { "sub", 3, null, "none" },
        { "sub", 3, null, "tens" },
    };

    [Theory]
    [MemberData(nameof(Rules))]
    public void Every_problem_has_three_distinct_options_ten_apart_with_exactly_one_answer(
        string kind, int digits, string? carry, string? borrow)
    {
        var rule = new PracticeRule(kind, digits, carry, borrow);
        for (var seed = 0; seed < Seeds; seed++)
        {
            var p = ProblemGenerator.Make(rule, new Random(seed));

            Assert.Equal(3, p.Options.Count);
            Assert.Equal(3, p.Options.Distinct().Count());
            Assert.Equal(p.Answer, p.Options[p.CorrectIndex]);
            Assert.Single(p.Options, o => o == p.Answer);
            Assert.All(p.Options, o => Assert.True(o > 0));
            foreach (var x in p.Options)
            {
                Assert.All(p.Options.Where(y => y != x), y => Assert.True(Math.Abs(x - y) >= ProblemGenerator.MinGap, $"{p.Question}: {x} vs {y}"));
            }
        }
    }

    [Theory]
    [MemberData(nameof(Rules))]
    public void The_correct_option_moves_around(string kind, int digits, string? carry, string? borrow)
    {
        var positions = Enumerable.Range(0, 60)
            .Select(seed => ProblemGenerator.Make(new PracticeRule(kind, digits, carry, borrow), new Random(seed)).CorrectIndex)
            .ToHashSet();

        Assert.Equal(3, positions.Count);
    }

    [Fact]
    public void Addition_follows_the_carry_rule_and_stays_within_999()
    {
        foreach (var (carry, check) in new (string, Func<int, int, bool>)[]
                 {
                     ("none", (a, b) => a % 10 + b % 10 < 10 && a / 10 % 10 + b / 10 % 10 < 10),
                     ("ones", (a, b) => a % 10 + b % 10 >= 10 && a / 10 % 10 + b / 10 % 10 + 1 < 10),
                 })
        {
            for (var seed = 0; seed < Seeds; seed++)
            {
                var p = ProblemGenerator.Make(new PracticeRule("add", 3, carry), new Random(seed));
                var (a, b) = (p.Numbers[0], p.Numbers[1]);

                Assert.Equal(a + b, p.Answer);
                Assert.True(p.Answer <= 999);
                Assert.True(check(a, b), $"{carry}: {p.Question}");
                Assert.Equal($"{a} + {b}", p.Question);
            }
        }
    }

    [Fact]
    public void Subtraction_with_borrowing_borrows_one_ten_and_never_across_a_zero()
    {
        for (var seed = 0; seed < Seeds; seed++)
        {
            var p = ProblemGenerator.Make(new PracticeRule("sub", 3, Borrow: "tens"), new Random(seed));
            var (a, b) = (p.Numbers[0], p.Numbers[1]);

            Assert.Equal(a - b, p.Answer);
            Assert.True(a <= 999);
            Assert.True(a % 10 < b % 10, p.Question);            // borrows in the ones
            Assert.True(a / 10 % 10 >= 1, p.Question);           // from a non-zero ten
            Assert.True(a / 10 % 10 - 1 >= b / 10 % 10, p.Question); // and only once
        }
    }

    [Fact]
    public void Subtraction_without_borrowing_never_needs_it()
    {
        for (var seed = 0; seed < Seeds; seed++)
        {
            var p = ProblemGenerator.Make(new PracticeRule("sub", 3, Borrow: "none"), new Random(seed));
            var (a, b) = (p.Numbers[0], p.Numbers[1]);

            Assert.True(a % 10 >= b % 10 && a / 10 % 10 >= b / 10 % 10, p.Question);
        }
    }

    [Fact]
    public void Place_value_and_expanded_form_questions_say_what_they_ask()
    {
        var expanded = ProblemGenerator.Make(new PracticeRule("expanded", 3), new Random(1));
        var face = ProblemGenerator.Make(new PracticeRule("placeValue", 3), new Random(1));
        var biggest = ProblemGenerator.Make(new PracticeRule("biggest", 3), new Random(1));

        Assert.Equal(expanded.Answer, expanded.Question.Split(" + ").Sum(int.Parse));
        var parts = face.Question.Split(" in ");
        var (digit, number) = (int.Parse(parts[0]), int.Parse(parts[1]));
        Assert.Contains(digit.ToString(), number.ToString());
        Assert.Contains(face.Answer, new[] { digit * 10, digit * 100 });
        Assert.Equal(biggest.Options.Max(), biggest.Answer);
    }

    [Fact]
    public void The_same_seed_gives_the_same_package_problems_and_the_activity_differs_from_the_check()
    {
        var first = PackagePractice.For(["M3.SUB.BORROW"], Book, "class|2026-10-19|M3.SUB.BORROW|te")!;
        var again = PackagePractice.For(["M3.SUB.BORROW"], Book, "class|2026-10-19|M3.SUB.BORROW|te")!;

        Assert.Equal(first.Check.Question, again.Check.Question);
        Assert.Equal(first.Check.Options, again.Check.Options);
        Assert.Equal(first.Activity.Question, again.Activity.Question);
        Assert.NotEqual(first.Activity.Question, first.Check.Question);
        Assert.Equal("M3.SUB.BORROW", first.CompetencyId);
        Assert.Null(PackagePractice.For(["M3.NO.RULE"], Book, "x"));
    }

    private static readonly CurriculumGraph Book = new(
    [
        new Competency("M3.SUB.BORROW", "ಕ", "Subtraction with borrowing", [], null, 5, 15, [], null,
            KeyWordsKn: ["ಕಳೆ", "ದಶಕ"], Practice: new PracticeRule("sub", 3, Borrow: "tens")),
        new Competency("M3.NO.RULE", "ಕ", "No rule", [], null, 5, 16, [], null),
    ]);
}
