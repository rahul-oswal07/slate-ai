using System.Security.Cryptography;
using System.Text;
using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.CatchUps.Practice;

/// <summary>The activity sum (P2) and the learning check (P3) for one catch-up. Both test the first target (§3.3).</summary>
public sealed record PackageProblems(string CompetencyId, Problem Activity, Problem Check);

public static class PackagePractice
{
    /// <summary>
    /// Problems for the first target that has a practice rule, or null if none does. <paramref name="seedKey"/> fixes
    /// the numbers (class, week, targets, language), so regenerating the words keeps the same sums.
    /// </summary>
    public static PackageProblems? For(IReadOnlyList<string> targets, CurriculumGraph curriculum, string seedKey)
    {
        var target = targets.Select(curriculum.Get).FirstOrDefault(c => c?.Practice is not null);
        if (target is null)
        {
            return null;
        }

        var rng = new Random(Seed(seedKey));
        var activity = ProblemGenerator.Make(target.Practice!, rng);
        Problem check;
        do
        {
            check = ProblemGenerator.Make(target.Practice!, rng);
        }
        while (check.Question == activity.Question);

        return new PackageProblems(target.Id, activity, check);
    }

    /// <summary>A stable seed (string.GetHashCode differs between runs).</summary>
    public static int Seed(string key) => BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0);
}
