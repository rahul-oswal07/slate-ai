using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;

namespace SlateAi.Api.Features.CatchUps;

/// <summary>
/// A catch-up's sections, named as today's message names them (T-103, D-129): the concept is A3's script with its audio,
/// the activity its instruction and other-objects line, the picture the learning check, the video the lesson's link, the
/// note hers.
/// </summary>
public static class CatchUpSectionKind
{
    public const string Concept = "concept";
    public const string Activity = "activity";
    public const string Picture = "picture";
    public const string Video = "video";
    public const string Note = "note";

    public static readonly string[] All = [Concept, Activity, Picture, Video, Note];

    /// <summary>
    /// The family's page is Listen → Activity → Answer (FR-10), and the answer updates the ledger (FR-11), so the concept,
    /// the activity and the picture stay. She can remove the video and her note.
    /// </summary>
    public static readonly string[] Removable = [Video, Note];

    /// <summary>What she can type into. The picture's numbers and answer come from code (guardrail 4).</summary>
    public static readonly string[] Editable = [Concept, Activity, Note];
}

/// <summary>The lesson's video link (from the admin page, put in by code, never the AI) and whether she removed it.</summary>
public sealed record CatchUpVideo(string Url, bool Removed);

/// <summary>Her note to the families, as typed, and whether she removed it.</summary>
public sealed record CatchUpNote(string Text, bool Removed);

/// <summary>Her words for one section: the script (concept), the instruction and other-objects line (activity) or her note.</summary>
public sealed record PackageEdit(string Kind, string? Text, string? ObjectOption = null);

/// <summary>Why her edit was not saved: the check's code, what exactly was wrong, and the numbers she may use.</summary>
public sealed record CatchUpEditProblem(string Problem, string? Detail, IReadOnlyList<string> Allowed);

/// <summary>
/// One change she makes: her words for a section, or a section removed or put back. <see cref="Review"/> is the review
/// after her words were checked (null: unchanged).
/// </summary>
public sealed record PackageRevision(
    string Kind, string? Text = null, string? ObjectOption = null, bool? Removed = null, PackageReview? Review = null);

/// <summary>
/// The checks her words go through before they are saved (T-103): the same code checks as A3's words (spec §5.4), never
/// an AI. Not empty; the length limits; no name, braces or signs; the family's script (her note may be in any language);
/// every sum right; only numbers the catch-up already uses, so the picture's answer is never given away (guardrail 4).
/// Pure.
/// </summary>
public static partial class CatchUpEditCheck
{
    /// <summary>A note is a line or two: as long as today's message's note (T-89).</summary>
    public const int MaxNoteWords = 40;

    public const int MaxNoteChars = 400;

    /// <summary>
    /// Null and the checks she passed (for the review) when her words can be saved; otherwise the problem. Problems:
    /// invalid-output (empty), too-long, name, placeholder, script, sum, numbers, activity-number.
    /// </summary>
    public static (CatchUpEditProblem? Problem, IReadOnlyList<VerifierCheck> Passed) Check(
        PackageView view, PackageEdit edit, CurriculumGraph curriculum, IReadOnlyList<string> names, VerifierOptions options)
    {
        var text = edit.Text?.Trim() ?? "";
        var other = edit.ObjectOption?.Trim() ?? "";
        if (text.Length == 0)
        {
            return Refused("invalid-output", "empty");
        }

        var texts = edit.Kind == CatchUpSectionKind.Activity && other.Length > 0 ? new[] { text, other } : [text];
        var words = Words(text);
        var seconds = (int)Math.Ceiling(words / options.WordsPerSecond.GetValueOrDefault(view.Language, 1.8));
        var tooLong = edit.Kind switch
        {
            CatchUpSectionKind.Concept => words > A3Validator.MaxScriptWords || text.Length > A3Validator.MaxScriptChars
                || seconds > options.MaxScriptSeconds,
            CatchUpSectionKind.Activity => texts.Any(t => t.Length > A3Validator.MaxField),
            _ => words > MaxNoteWords || text.Length > MaxNoteChars,
        };
        if (tooLong)
        {
            return Refused("too-long", edit.Kind == CatchUpSectionKind.Concept ? $"about {seconds} seconds, limit {options.MaxScriptSeconds}" : null);
        }

        if (A3Validator.UnsafeText(texts, names) is { } unsafeText)
        {
            // Never echo a name back: only the kind of problem.
            return Refused(unsafeText.Replace("fallback:", ""), null);
        }

        // Her note may be in any language: it goes to the family as she wrote it (D-129). The script and the activity are
        // read (and the script heard) in the family's language.
        if (edit.Kind != CatchUpSectionKind.Note
            && texts.Any(t => HasLetters(t) && !(CodeChecks.ScriptShare(t, view.Language) is var (share, _) && share >= options.MinScriptShare)))
        {
            return Refused("script", view.Language);
        }

        if (texts.Select(CodeChecks.WrongSum).OfType<string>().FirstOrDefault() is { } wrong)
        {
            return Refused("sum", wrong);
        }

        var allowed = AllowedNumbers(view, curriculum);
        var extra = texts.SelectMany(t => Digits().Matches(t)).Select(m => m.Value).Where(n => !allowed.Contains(n)).Distinct().ToList();
        if (extra.Count > 0)
        {
            return (new CatchUpEditProblem(
                "numbers", string.Join(", ", extra), allowed.OrderBy(n => n.Length).ThenBy(n => n, StringComparer.Ordinal).ToList()), []);
        }

        if (edit.Kind == CatchUpSectionKind.Concept)
        {
            // The activity starts from the script's numbers (A4's numbers check): her script keeps them.
            var missing = view.Activity.Numbers.Select(n => n.ToString(CultureInfo.InvariantCulture))
                .Where(n => !Regex.IsMatch(text, $"(?<!\\d){n}(?!\\d)")).ToList();
            if (missing.Count > 0)
            {
                return Refused("activity-number", string.Join(", ", missing));
            }

            return (null,
            [
                new VerifierCheck(CodeChecks.Numbers, true, "her words: sums right, the activity's numbers kept"),
                new VerifierCheck(CodeChecks.ScriptBlock, true, $"her words, in the family's script"),
                new VerifierCheck(CodeChecks.Length, true, $"about {seconds} seconds, limit {options.MaxScriptSeconds}"),
                new VerifierCheck(CodeChecks.NamesCheck, true, "no names"),
            ]);
        }

        return edit.Kind == CatchUpSectionKind.Activity
            ? (null,
            [
                new VerifierCheck(CodeChecks.Numbers, true, "her words: sums right"),
                new VerifierCheck(CodeChecks.ScriptBlock, true, "her words, in the family's script"),
                new VerifierCheck(CodeChecks.NamesCheck, true, "no names"),
            ])
            : (null, []);
    }

    /// <summary>
    /// The numbers her words may use: 0 to 10 and 100 (counting, a bundle), the textbook pages, every number the catch-up's
    /// words and questions already use, and the activity's numbers with their hundreds, tens and ones. Never the
    /// picture's answer or its other choices (above 10), unless the activity uses the same number.
    /// </summary>
    public static HashSet<string> AllowedNumbers(PackageView view, CurriculumGraph curriculum)
    {
        static string S(int n) => n.ToString(CultureInfo.InvariantCulture);
        var allowed = Enumerable.Range(0, 11).Select(S).Append("100").ToHashSet(StringComparer.Ordinal);
        var activity = view.Activity;
        var check = view.Check;
        var open = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in activity.Numbers.Append(activity.Answer))
        {
            foreach (var part in new[] { n, n / 100, n / 10 % 10, n % 10, n / 10 })
            {
                open.Add(S(part));
            }
        }

        foreach (var text in new[] { activity.Question, check.Question })
        {
            foreach (Match m in Digits().Matches(text))
            {
                open.Add(m.Value);
            }
        }

        foreach (var page in view.TargetCompetencyIds.Select(curriculum.Get).OfType<Competency>().Select(c => c.TextbookPage).OfType<TextbookPage>())
        {
            open.Add(S(page.Page));
        }

        var secret = check.Options.Append(check.Answer).Select(S).Where(n => !open.Contains(n)).ToHashSet(StringComparer.Ordinal);
        foreach (var text in new[] { view.Script, activity.Instruction, activity.ObjectOption, check.Prompt }.OfType<string>())
        {
            foreach (Match m in Digits().Matches(text))
            {
                if (!secret.Contains(m.Value))
                {
                    allowed.Add(m.Value);
                }
            }
        }

        allowed.UnionWith(open);
        return allowed;
    }

    /// <summary>
    /// The review after her words: the checks she just passed replace the earlier ones of the same name. Her script is her
    /// own, so A4's content check and the key-word count (about A3's words) go, and so does the audio until it is voiced
    /// again. Unchanged for a note.
    /// </summary>
    public static PackageReview? Review(PackageReview? before, string kind, IReadOnlyList<VerifierCheck> passed)
    {
        if (passed.Count == 0)
        {
            return before;
        }

        string[] gone = kind == CatchUpSectionKind.Concept ? [ContentCheck.Name, CodeChecks.KeyWords, PackageAudio.CheckName] : [];
        var checks = (before?.Checks ?? [])
            .Where(c => !gone.Contains(c.Name) && passed.All(p => p.Name != c.Name))
            .Concat(passed)
            .ToList();
        return new PackageReview(checks.All(c => c.Passed), before?.Attempts ?? 0, checks);
    }

    private static (CatchUpEditProblem?, IReadOnlyList<VerifierCheck>) Refused(string problem, string? detail) =>
        (new CatchUpEditProblem(problem, detail, []), []);

    private static bool HasLetters(string text) => text.Any(char.IsLetter);

    private static int Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();
}

/// <summary>
/// How her change lands on a catch-up (T-103, guardrail 3). Every change is a new version and clears the approval, so she
/// ticks and approves again; nothing about the old version can go. Pure: the store and its test fake both use it.
/// </summary>
public static class CatchUpRevision
{
    /// <summary>The hint while her edited script is being voiced: the writer makes audio, not new words.</summary>
    public const string VoiceHint = "voice";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>She can change a catch-up she can read and that hasn't gone: ready, needs review, or approved but not sent.</summary>
    public static bool CanChange(string status) =>
        status is PackageStatus.Ready or PackageStatus.NeedsReview or PackageStatus.Approved;

    /// <summary>Whether this change fits the catch-up: there is a section to remove, and her words have somewhere to go.</summary>
    public static bool Fits(CatchUpPackage package, PackageRevision revision) => revision switch
    {
        { Removed: not null, Kind: CatchUpSectionKind.Video } => package.VideoUrl is not null,
        { Removed: not null, Kind: CatchUpSectionKind.Note } => package.TeacherNote is not null,
        { Removed: not null } => false,
        { Kind: CatchUpSectionKind.Concept or CatchUpSectionKind.Activity } => package.Script is not null,
        { Kind: CatchUpSectionKind.Note } => true,
        _ => false,
    };

    /// <summary>
    /// Applies the change: a new version with the approval cleared. Her script goes back to generating to be voiced again
    /// (the old audio and A3's Kannada summary no longer match it, so both go); anything else is ready again, or needs
    /// review when a check still fails.
    /// </summary>
    public static void Apply(CatchUpPackage package, PackageRevision revision, DateTimeOffset now)
    {
        var voice = false;
        if (revision.Removed is { } removed)
        {
            package.RemovedSections = removed
                ? [.. package.RemovedSections.Append(revision.Kind).Distinct()]
                : [.. package.RemovedSections.Where(k => k != revision.Kind)];
        }
        else
        {
            switch (revision.Kind)
            {
                case CatchUpSectionKind.Concept:
                    package.Script = revision.Text;
                    package.GlossKn = null;
                    package.GlossEn = null;
                    package.AudioSegments = null;
                    package.AudioUrl = null;
                    package.TeacherEdited = true;
                    voice = true;
                    break;
                case CatchUpSectionKind.Activity:
                    var activity = JsonSerializer.Deserialize<ActivityData>(package.Activity, Json)!;
                    package.Activity = JsonSerializer.Serialize(
                        activity with { Instruction = revision.Text, ObjectOption = string.IsNullOrWhiteSpace(revision.ObjectOption) ? null : revision.ObjectOption },
                        Json);
                    package.TeacherEdited = true;
                    break;
                case CatchUpSectionKind.Note:
                    package.TeacherNote = revision.Text;
                    break;
            }
        }

        if (revision.Review is { } review)
        {
            package.Review = JsonSerializer.Serialize(review, Json);
        }

        var current = package.Review is null ? null : JsonSerializer.Deserialize<PackageReview>(package.Review, Json);
        var stillFailing = current is { Passed: false } || (current is null && package.Status == PackageStatus.NeedsReview);
        package.ScriptVersion++;
        package.ApprovedAt = null;
        package.ApprovedByTeacherId = null;
        package.ApprovedScriptVersion = null;
        package.Status = voice ? PackageStatus.Generating : stillFailing ? PackageStatus.NeedsReview : PackageStatus.Ready;
        package.Hint = voice ? VoiceHint : package.Hint;
        if (voice)
        {
            package.Verdict = "edited";
        }

        package.UpdatedAt = now;
    }

    /// <summary>
    /// Her edited script, voiced (T-103): the new audio and the review with its audio check; ready when every check passes,
    /// otherwise needs review. The version stays: it was raised when she saved.
    /// </summary>
    public static void Voiced(CatchUpPackage package, AudioSegments? audio, PackageReview review, DateTimeOffset now)
    {
        package.AudioSegments = audio is null ? null : JsonSerializer.Serialize(audio, Json);
        package.AudioUrl = audio?.Segments.LastOrDefault(s => s.Kind == AudioKind.Script)?.Url;
        package.Review = JsonSerializer.Serialize(review, Json);
        package.Status = review.Passed ? PackageStatus.Ready : PackageStatus.NeedsReview;
        package.Verdict = review.Passed ? "edited" : new VerifierResult(false, review.Checks).Verdict;
        package.Hint = null;
        package.UpdatedAt = now;
    }

    /// <summary>The lesson's video as the teacher reads it, or null.</summary>
    public static CatchUpVideo? Video(CatchUpPackage package) =>
        package.VideoUrl is { } url ? new CatchUpVideo(url, package.RemovedSections.Contains(CatchUpSectionKind.Video)) : null;

    /// <summary>Her note as the teacher reads it, or null.</summary>
    public static CatchUpNote? Note(CatchUpPackage package) =>
        package.TeacherNote is { } note ? new CatchUpNote(note, package.RemovedSections.Contains(CatchUpSectionKind.Note)) : null;

    /// <summary>What the family is sent besides the lesson (T-103): the video and her note, unless she removed them.</summary>
    public static (string? VideoUrl, string? Note) ForFamily(string? videoUrl, string? note, IReadOnlyList<string> removed) => (
        removed.Contains(CatchUpSectionKind.Video) ? null : videoUrl,
        removed.Contains(CatchUpSectionKind.Note) ? null : note);
}
