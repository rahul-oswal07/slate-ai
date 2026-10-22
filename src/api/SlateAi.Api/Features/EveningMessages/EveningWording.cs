using System.Text.Json;

namespace SlateAi.Api.Features.EveningMessages;

public sealed class EveningOptions
{
    public const string Section = "EveningMessages";

    /// <summary>Evening messages go after this time, IST (FR-20: after 6 pm).</summary>
    public TimeOnly SendAt { get; set; } = new(18, 0);

    /// <summary>Days a parent link keeps working (spec §4.4: 14 days).</summary>
    public int LinkDays { get; set; } = 14;

    /// <summary>The wording the teacher approves, relative to the app's output directory.</summary>
    public string WordingFile { get; set; } = "data/seed/evening-message.json";

    /// <summary>Runs the 6 pm job in the background. Tests turn it off so they never touch a database.</summary>
    public bool SchedulerEnabled { get; set; } = true;
}

/// <summary>One language's template: text with {child} {lesson} {page}, and how a page is written.</summary>
public sealed record LanguageWording(string Text, string Page, string PageNoPart);

/// <summary>The evening-message wording file (<c>data/seed/evening-message.json</c>), loaded once.</summary>
public sealed record EveningWording(string Version, IReadOnlyDictionary<string, LanguageWording> Languages)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static EveningWording Load(string path)
    {
        var file = JsonSerializer.Deserialize<EveningWording>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException($"Evening-message wording '{path}' is empty.");
        if (string.IsNullOrWhiteSpace(file.Version) || file.Languages is not { Count: > 0 })
        {
            throw new InvalidOperationException($"Evening-message wording '{path}' needs a version and languages.");
        }

        return file;
    }
}
