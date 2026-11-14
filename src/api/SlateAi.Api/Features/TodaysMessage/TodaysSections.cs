using System.Text.Json;
using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.TodaysMessage;

public static class TodaysMessageStatus
{
    public const string Generating = "generating";
    public const string Ready = "ready";
    public const string Approved = "approved";
    public const string Failed = "failed";
}

/// <summary>The section names, as the API and the editor (T-89) use them.</summary>
public static class TodaysSectionKind
{
    public const string Concept = "concept";
    public const string Activity = "activity";
    public const string Picture = "picture";
    public const string Video = "video";
    public const string Note = "note";

    public static readonly string[] All = [Concept, Activity, Picture, Video, Note];
}

/// <summary>A short explanation of the lesson, per language, with its voiced clip (D-91: text and audio).</summary>
public sealed record TodaysConcept(
    IReadOnlyDictionary<string, string> Text, IReadOnlyDictionary<string, Guid> Clips, bool Removed = false);

/// <summary>Up to 3 parent-led steps, per language.</summary>
public sealed record TodaysActivity(IReadOnlyDictionary<string, IReadOnlyList<string>> Steps, bool Removed = false);

/// <summary>
/// The picture question, numbers and answer by code from the book's list (D-93, guardrail 4). Kind "compare": the child
/// picks &gt;, &lt; or = between <see cref="A"/> and <see cref="B"/> (T-90 draws them as bundles and loose sticks).
/// </summary>
public sealed record TodaysPicture(string Kind, int A, int B, string Answer, bool Removed = false)
{
    public const string Compare = "compare";
}

/// <summary>One checked DIKSHA link, chosen by code (T-94); null until then.</summary>
public sealed record TodaysVideo(string Url, bool Removed = false);

/// <summary>Her own note (T-89): her Kannada word for word, and A9's version for each other language; null until she adds one.</summary>
public sealed record TodaysNote(IReadOnlyDictionary<string, string> Text, bool Removed = false);

/// <summary>Everything a today's message holds. Stored as JSON on the row; never holds a name or a child ID.</summary>
/// <param name="Teacher">The Kannada is hers (she edited it, D-92): a new draft keeps it and A9 only writes the other languages.</param>
public sealed record TodaysSections(
    TodaysConcept Concept,
    TodaysActivity Activity,
    TodaysPicture Picture,
    TodaysVideo? Video = null,
    TodaysNote? Note = null,
    bool Teacher = false)
{
    /// <summary>Her Kannada as it stands: what an edit starts from and what A9 translates (D-92).</summary>
    public TeacherKannada Kannada => new(
        Concept.Text.GetValueOrDefault("kn") ?? "",
        Activity.Steps.GetValueOrDefault("kn") ?? [],
        Note?.Text.GetValueOrDefault("kn"));

    /// <summary>Her edit applied to the Kannada of one section; null for a section she can't type into.</summary>
    public TodaysSections? WithKannada(string kind, string? text, IReadOnlyList<string>? steps) => kind switch
    {
        TodaysSectionKind.Concept when text is not null =>
            this with { Concept = Concept with { Text = With(Concept.Text, text) }, Teacher = true },
        TodaysSectionKind.Activity when steps is not null =>
            this with { Activity = Activity with { Steps = With(Activity.Steps, steps) }, Teacher = true },
        TodaysSectionKind.Note when text is not null =>
            this with { Note = new TodaysNote(With(Note?.Text ?? new Dictionary<string, string>(), text)), Teacher = true },
        _ => null,
    };

    private static Dictionary<string, T> With<T>(IReadOnlyDictionary<string, T> values, T kannada)
    {
        var copy = values.ToDictionary(v => v.Key, v => v.Value);
        copy["kn"] = kannada;
        return copy;
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Null for the "null" a generating row holds before its first draft.</summary>
    public static TodaysSections? Parse(string json) => JsonSerializer.Deserialize<TodaysSections>(json, Json);

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>The languages it is written in: Kannada (hers) and each absent family's.</summary>
    public IReadOnlyList<string> Languages => Concept.Text.Keys.ToList();

    /// <summary>Removes or restores one section; null when there is no such section to change.</summary>
    public TodaysSections? WithRemoved(string kind, bool removed) => kind switch
    {
        TodaysSectionKind.Concept => this with { Concept = Concept with { Removed = removed } },
        TodaysSectionKind.Activity => this with { Activity = Activity with { Removed = removed } },
        TodaysSectionKind.Picture => this with { Picture = Picture with { Removed = removed } },
        TodaysSectionKind.Video when Video is not null => this with { Video = Video with { Removed = removed } },
        TodaysSectionKind.Note when Note is not null => this with { Note = Note with { Removed = removed } },
        _ => null,
    };

    /// <summary>At least one section still goes: removing the last one is refused (there would be nothing to approve).</summary>
    public bool HasAnySection =>
        !Concept.Removed || !Activity.Removed || !Picture.Removed || Video is { Removed: false } || Note is { Removed: false };
}

/// <summary>Picks the picture question's numbers from the book's exercise list (D-93). Pure; the AI never chooses them.</summary>
public static class TodaysPicturePicker
{
    /// <summary>
    /// The <paramref name="index"/>th "compare" pair of the first lesson that has one (wrapping round), with its answer by
    /// <see cref="ExerciseAnswers.CompareSign"/>; null when no lesson has a compare list.
    /// </summary>
    public static TodaysPicture? Pick(IReadOnlyList<Competency> lessons, int index = 0)
    {
        var pairs = lessons.Select(l => l.Exercises?.Compare).FirstOrDefault(c => c is { Count: > 0 });
        if (pairs is null)
        {
            return null;
        }

        var pair = pairs[((index % pairs.Count) + pairs.Count) % pairs.Count];
        return new TodaysPicture(TodaysPicture.Compare, pair[0], pair[1], ExerciseAnswers.CompareSign(pair[0], pair[1]));
    }

    /// <summary>Whether a lesson set can have today's message: one of them has the book's exercise list (D-93).</summary>
    public static bool Supported(IReadOnlyList<Competency> lessons) => Pick(lessons) is not null;
}

/// <summary>What a family is sent (D-91): their language only, and nothing she removed. Pure.</summary>
public static class TodaysFamilyView
{
    public static EveningMessages.ParentTodaysMessage For(TodaysSections sections, string language) => new(
        sections.Concept.Removed ? null : sections.Concept.Text.GetValueOrDefault(language),
        sections.Activity.Removed ? null : sections.Activity.Steps.GetValueOrDefault(language),
        sections.Picture.Removed ? null : new EveningMessages.ParentPicture(sections.Picture.A, sections.Picture.B),
        sections.Video is { Removed: false } video ? video : null,
        sections.Note is { Removed: false } note ? note.Text.GetValueOrDefault(language) : null);
}
