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

    /// <summary>
    /// What the approved WhatsApp text promises the family ("the lesson audio, a home activity and a picture question
    /// are ready"), so these can't be removed (D-108); she can still edit the text and choose another picture pair.
    /// </summary>
    public static readonly string[] Promised = [Concept, Activity, Picture];

    /// <summary>
    /// What a given message promises: the picture only when it has one. A lesson without the book's exercises has no
    /// picture question (T-139, D-121), so only the concept and the activity can't be removed.
    /// </summary>
    public static IReadOnlyList<string> PromisedFor(TodaysSections sections) =>
        sections.Picture is null ? [Concept, Activity] : Promised;
}

/// <summary>A short explanation of the lesson, per language, with its voiced clip (D-91: text and audio).</summary>
public sealed record TodaysConcept(
    IReadOnlyDictionary<string, string> Text, IReadOnlyDictionary<string, Guid> Clips, bool Removed = false);

/// <summary>Up to 3 parent-led steps, per language.</summary>
/// <param name="Clips">The steps voiced, one clip per language (T-120), so a parent who doesn't read gets the activity too. A
/// language whose steps were too long to voice has no clip and keeps the text. Null on drafts made before T-120.</param>
public sealed record TodaysActivity(
    IReadOnlyDictionary<string, IReadOnlyList<string>> Steps, bool Removed = false, IReadOnlyDictionary<string, Guid>? Clips = null);

/// <summary>
/// The picture question, numbers and answer by code from the book's list (D-93, guardrail 4). Kind "compare": the child
/// picks &gt;, &lt; or = between <see cref="A"/> and <see cref="B"/> (T-90 draws them as bundles and loose sticks).
/// Kind "add" or "sub" (D-116): the child works out A + B or A − B and picks one of <see cref="Options"/>, drawn as
/// sticks; <see cref="Answer"/> is then the number as text ("686").
/// </summary>
/// <param name="Options">The three numbers to choose from for a sum, in the order shown; null for compare.</param>
public sealed record TodaysPicture(string Kind, int A, int B, string Answer, bool Removed = false, IReadOnlyList<int>? Options = null)
{
    public const string Compare = "compare";
    public const string Add = "add";
    public const string Sub = "sub";

    public bool IsSum => Kind is Add or Sub;
}

/// <summary>One checked DIKSHA link, chosen by code (T-94); null until then.</summary>
public sealed record TodaysVideo(string Url, bool Removed = false);

/// <summary>
/// Her own note (T-89), null until she adds one. She may write it in any language (T-119, D-111): <see cref="Text"/> is
/// what each family reads, in its home language, A9's translation of <see cref="Original"/>; a note she wrote in Kannada
/// goes to Kannada families word for word.
/// </summary>
/// <param name="Original">What she typed; null for a note saved before T-119, which was her Kannada (<c>Text["kn"]</c>).</param>
public sealed record TodaysNote(IReadOnlyDictionary<string, string> Text, bool Removed = false, string? Original = null);

/// <summary>Everything a today's message holds. Stored as JSON on the row; never holds a name or a child ID.</summary>
/// <param name="Picture">The picture question; null for a lesson without the book's exercises (T-139, D-121).</param>
/// <param name="Teacher">The Kannada is hers (she edited it, D-92): a new draft keeps it and A9 only writes the other languages.</param>
public sealed record TodaysSections(
    TodaysConcept Concept,
    TodaysActivity Activity,
    TodaysPicture? Picture,
    TodaysVideo? Video = null,
    TodaysNote? Note = null,
    bool Teacher = false)
{
    /// <summary>
    /// Her Kannada as it stands: what an edit starts from and what A9 translates (D-92). The note is what she typed, in
    /// whatever language she wrote it (T-119).
    /// </summary>
    public TeacherKannada Kannada => new(
        Concept.Text.GetValueOrDefault("kn") ?? "",
        Activity.Steps.GetValueOrDefault("kn") ?? [],
        Note?.Original ?? Note?.Text.GetValueOrDefault("kn"));

    /// <summary>Her edit applied to the Kannada of one section; null for a section she can't type into.</summary>
    public TodaysSections? WithKannada(string kind, string? text, IReadOnlyList<string>? steps) => kind switch
    {
        TodaysSectionKind.Concept when text is not null =>
            this with { Concept = Concept with { Text = With(Concept.Text, text) }, Teacher = true },
        TodaysSectionKind.Activity when steps is not null =>
            this with { Activity = Activity with { Steps = With(Activity.Steps, steps) }, Teacher = true },
        // Kept as typed; the next draft writes it in every family's language, Kannada included unless she wrote Kannada.
        TodaysSectionKind.Note when text is not null =>
            this with { Note = new TodaysNote(With(Note?.Text ?? new Dictionary<string, string>(), text), Original: text), Teacher = true },
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
        TodaysSectionKind.Picture when Picture is not null => this with { Picture = Picture with { Removed = removed } },
        TodaysSectionKind.Video when Video is not null => this with { Video = Video with { Removed = removed } },
        TodaysSectionKind.Note when Note is not null => this with { Note = Note with { Removed = removed } },
        _ => null,
    };

    /// <summary>At least one section still goes: removing the last one is refused (there would be nothing to approve).</summary>
    public bool HasAnySection =>
        !Concept.Removed || !Activity.Removed || Picture is { Removed: false } || Video is { Removed: false } || Note is { Removed: false };
}

/// <summary>Picks the picture question's numbers from the book's exercise list (D-93). Pure; the AI never chooses them.</summary>
public static class TodaysPicturePicker
{
    /// <summary>
    /// The <paramref name="index"/>th "compare" pair of the first lesson that has one (wrapping round), with its answer by
    /// <see cref="ExerciseAnswers.CompareSign"/>; null when no lesson has a compare list.
    /// </summary>
    /// <param name="arithmetic">
    /// <c>TodaysMessage:ArithmeticExercises</c> (D-116): with it, a lesson set with no compare pairs uses the first lesson's
    /// add pairs, else its sub pairs. Compare always comes first, so the Nalikali lesson is unchanged.
    /// </param>
    public static TodaysPicture? Pick(IReadOnlyList<Competency> lessons, int index = 0, bool arithmetic = false)
    {
        var (kind, pairs) = Pairs(lessons, arithmetic);
        if (pairs.Count == 0)
        {
            return null;
        }

        var pair = pairs[((index % pairs.Count) + pairs.Count) % pairs.Count];
        return kind == TodaysPicture.Compare
            ? new TodaysPicture(TodaysPicture.Compare, pair[0], pair[1], ExerciseAnswers.CompareSign(pair[0], pair[1]))
            : new TodaysPicture(
                kind, pair[0], pair[1], ExerciseAnswers.Answer(kind, pair[0], pair[1]).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Options: ExerciseAnswers.Choices(kind, pair[0], pair[1]));
    }

    /// <summary>The book's pairs the picture question can use, with their kind; empty when there are none.</summary>
    public static (string Kind, IReadOnlyList<IReadOnlyList<int>> Pairs) Pairs(IReadOnlyList<Competency> lessons, bool arithmetic = false)
    {
        if (lessons.Select(l => l.Exercises?.Compare).FirstOrDefault(c => c is { Count: > 0 }) is { } compare)
        {
            return (TodaysPicture.Compare, compare);
        }

        if (arithmetic)
        {
            foreach (var lesson in lessons)
            {
                if (lesson.Exercises?.Add is { Count: > 0 } add)
                {
                    return (TodaysPicture.Add, add);
                }

                if (lesson.Exercises?.Sub is { Count: > 0 } sub)
                {
                    return (TodaysPicture.Sub, sub);
                }
            }
        }

        return (TodaysPicture.Compare, []);
    }

    /// <summary>Whether a lesson set can have today's message: one of them has the book's exercise list (D-93, D-116).</summary>
    public static bool Supported(IReadOnlyList<Competency> lessons, bool arithmetic = false) => Pick(lessons, 0, arithmetic) is not null;
}

/// <summary>What a family is sent (D-91): their language only, and nothing she removed. Pure.</summary>
public static class TodaysFamilyView
{
    public static EveningMessages.ParentTodaysMessage For(TodaysSections sections, string language) => new(
        sections.Concept.Removed ? null : sections.Concept.Text.GetValueOrDefault(language),
        sections.Activity.Removed ? null : sections.Activity.Steps.GetValueOrDefault(language),
        // None for a lesson without the book's exercises (T-139), or when she removed it.
        sections.Picture is not { Removed: false } picture
            ? null
            : new EveningMessages.ParentPicture(picture.A, picture.B, Kind: picture.Kind, Options: picture.Options),
        sections.Video is { Removed: false } video ? video : null,
        sections.Note is { Removed: false } note ? note.Text.GetValueOrDefault(language) : null);
}
