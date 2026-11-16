using System.Text.Json;

namespace SlateAi.Api.Features.WeeklyStar;

/// <summary>One language's weekly star wording: the WhatsApp body, the page's message and its badge card, with {child}.</summary>
public sealed record StarLanguageWording(string WhatsApp, string Message, string Badge);

/// <summary>
/// The weekly star wording (<c>data/seed/weekly-star.json</c>, T-125, D-118), loaded once. The teacher approves a
/// version; code fills only {child} (guardrail 3, 4). A new version asks her again (D-49).
/// </summary>
public sealed record WeeklyStarWording(string Version, IReadOnlyDictionary<string, StarLanguageWording> Languages)
{
    public const string ChildSlot = "{child}";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static WeeklyStarWording Load(string path)
    {
        var file = JsonSerializer.Deserialize<WeeklyStarWording>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException($"Weekly star wording '{path}' is empty.");
        if (string.IsNullOrWhiteSpace(file.Version) || file.Languages is not { Count: > 0 })
        {
            throw new InvalidOperationException($"Weekly star wording '{path}' needs a version and languages.");
        }

        foreach (var (language, text) in file.Languages)
        {
            if (new[] { text.WhatsApp, text.Message, text.Badge }.Any(t => string.IsNullOrWhiteSpace(t) || !t.Contains(ChildSlot)))
            {
                throw new InvalidOperationException($"Weekly star wording '{path}' ({language}): every text needs {ChildSlot}.");
            }
        }

        return file;
    }

    /// <summary>The text with the child's first name in it. Nothing else is filled.</summary>
    public static string Fill(string text, string childName) => text.Replace(ChildSlot, childName, StringComparison.Ordinal);
}

public sealed class WeeklyStarOptions
{
    public const string Section = "WeeklyStar";

    /// <summary>The wording the teacher approves, relative to the app's output directory.</summary>
    public string WordingFile { get; set; } = "data/seed/weekly-star.json";

    /// <summary>Days the star page link keeps working, as the other family links (spec §4.4).</summary>
    public int LinkDays { get; set; } = 14;
}
