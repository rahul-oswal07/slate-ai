using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Tests.Features.LessonContents;

/// <summary>
/// <c>data/lesson-content/grade3-maths.json</c> (T-141, D-123): what A9 builds today's message's spoken concept on. It
/// must cover the curriculum, pass the admin page's rules, get every sum right, never give away a picture question
/// (the book's exercise pairs) and name no child of the demo class.
/// </summary>
public sealed partial class LessonContentFileTests
{
    private static readonly string Dir = AppContext.BaseDirectory;
    private static readonly CurriculumGraph Curriculum = CurriculumLoader.LoadDirectory(Path.Combine(Dir, "data", "curriculum"));
    private static readonly LessonContentFile File = LessonContentFile.Load(Path.Combine(Dir, "data", "lesson-content", "grade3-maths.json"));

    [Fact]
    public void Every_lesson_of_the_curriculum_is_listed_once_and_passes_the_rules()
    {
        var (entries, problems) = File.Check(Curriculum);

        Assert.Empty(problems);
        Assert.Equal(Curriculum.All.Select(c => c.Id).Order(), entries.Select(e => e.CompetencyId).Order());
        Assert.All(File.Lessons, l => Assert.Contains(l.Source, new[] { LessonContentSources.Textbook, LessonContentSources.Written }));
    }

    [Fact]
    public void Every_sum_in_it_is_right()
    {
        var checkedSums = 0;
        foreach (var lesson in File.Lessons)
        {
            foreach (var text in lesson.Examples.Prepend(lesson.Description))
            {
                // A chain like "10 = 9 + 1 = 8 + 2" or "2 + 3 + 1 = 6": every side with a sign must equal the others.
                foreach (Match chain in Chain().Matches(Plain(text)))
                {
                    // From the first side with a sign: "₹7 = 7 × 100 = 700 paise" starts in another unit, "₹1 = 100 paise" is no sum.
                    var sides = chain.Value.Split('=').Select(s => s.Trim()).SkipWhile(s => !Operator().IsMatch(s)).ToList();
                    if (sides.Count < 2)
                    {
                        continue;
                    }

                    var values = sides.Select(Evaluate).ToList();
                    Assert.True(values.All(v => Math.Abs(v - values[0]) < 1e-9), $"{lesson.Id}: {chain.Value}");
                    checkedSums++;
                }
            }
        }

        Assert.True(checkedSums > 50, $"only {checkedSums} sums found: is the pattern still right?");
    }

    [Fact]
    public void No_worked_example_uses_a_pair_of_the_books_exercises()
    {
        // The picture question comes from these pairs (D-93, D-116); an example with both numbers could give it away.
        foreach (var lesson in Curriculum.All.Where(c => c.Exercises is not null))
        {
            var pairs = new[] { lesson.Exercises!.Compare, lesson.Exercises.Add, lesson.Exercises.Sub }
                .SelectMany(p => p ?? []).Where(p => p.Count == 2);
            var examples = File.Lessons.Single(l => l.Id == lesson.Id).Examples;
            foreach (var pair in pairs)
            {
                Assert.DoesNotContain(examples, e =>
                    Numbers(e).Contains(pair[0].ToString(CultureInfo.InvariantCulture))
                    && Numbers(e).Contains(pair[1].ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

    [Fact]
    public void No_child_of_the_demo_class_is_named()
    {
        using var seed = JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(Dir, "data", "seed", "demo-class.json")));
        var names = seed.RootElement.GetProperty("children").EnumerateArray().Select(c => c.GetProperty("firstName").GetString()!).ToList();

        foreach (var lesson in File.Lessons)
        {
            foreach (var text in lesson.Examples.Prepend(lesson.Description))
            {
                Assert.DoesNotContain(names, n => Regex.IsMatch(text, $@"\b{Regex.Escape(n)}\b", RegexOptions.IgnoreCase));
            }
        }
    }

    private static string Plain(string text) =>
        text.Replace('−', '-').Replace('×', '*').Replace('÷', '/').Replace("₹", "");

    private static double Evaluate(string side) =>
        Convert.ToDouble(new DataTable().Compute(side, null), CultureInfo.InvariantCulture);

    private static HashSet<string> Numbers(string text) => [.. Digits().Matches(text).Select(m => m.Value)];

    /// <summary>Two or more sides joined by "=", at least one with a sign; numbers may have paise (26.40).</summary>
    [GeneratedRegex(@"\d+(?:\.\d+)?(?:\s*[-+*/]\s*\d+(?:\.\d+)?)*(?:\s*=\s*\d+(?:\.\d+)?(?:\s*[-+*/]\s*\d+(?:\.\d+)?)*)+")]
    private static partial Regex Chain();

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();

    [GeneratedRegex(@"\d\s*[-+*/]\s*\d")]
    private static partial Regex Operator();
}
