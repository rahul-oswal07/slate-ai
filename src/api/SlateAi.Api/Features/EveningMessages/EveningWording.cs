using System.Text.Json;

namespace SlateAi.Api.Features.EveningMessages;

public sealed class EveningOptions
{
    public const string Section = "EveningMessages";

    /// <summary>Evening messages go after this time, IST (FR-20: after 6 pm).</summary>
    public TimeOnly SendAt { get; set; } = new(18, 0);

    /// <summary>
    /// How long the 6 pm job waits for her to check a ready lesson clip, from when it was ready or from
    /// <see cref="SendAt"/> if it was ready earlier (T-85). After it the plain message goes.
    /// </summary>
    public TimeSpan LessonAudioWait { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long the 6 pm job waits for her to approve a ready today's message, from when it was ready or from
    /// <see cref="SendAt"/> if it was ready earlier (T-111, D-100). After it the plain message goes.
    /// </summary>
    public TimeSpan TodaysMessageWait { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Days a parent link keeps working (spec §4.4: 14 days).</summary>
    public int LinkDays { get; set; } = 14;

    /// <summary>The wording the teacher approves, relative to the app's output directory.</summary>
    public string WordingFile { get; set; } = "data/seed/evening-message.json";

    /// <summary>Runs the 6 pm job in the background. Tests turn it off so they never touch a database.</summary>
    public bool SchedulerEnabled { get; set; } = true;
}

/// <summary>
/// One language's template: text with {child} {lesson} {page}, and how a page is written. Since wording v4 (T-101,
/// D-99) the page formats start with {book}, which code fills from <paramref name="Books"/>.
/// </summary>
/// <param name="Books">
/// The book's name by <c>TextbookPage.Book</c> ("textbook", "nalikali"). It lives in the wording file only, not on
/// the class's <c>MessageTemplate</c> row: see <see cref="EveningWording.WithBooks"/>.
/// </param>
public sealed record LanguageWording(
    string Text, string Page, string PageNoPart, IReadOnlyDictionary<string, string>? Books = null);

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

    /// <summary>A class's approved row has the text and page formats; the book names come from this file.</summary>
    public LanguageWording WithBooks(LanguageWording row, string language) =>
        row with { Books = Languages.GetValueOrDefault(language)?.Books };
}
