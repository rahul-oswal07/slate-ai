using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>One language of today's message as she (and later the family page) reads it.</summary>
/// <param name="ConceptClipUrl">The voiced concept (<c>/api/audio/{id}</c>), or null.</param>
public sealed record TodaysLanguageView(string Language, int Families, string Concept, string? ConceptClipUrl, IReadOnlyList<string> Steps);

/// <summary>Today's message for the Approve tab (T-89). Numbers come from code; the text from A9, checked by code.</summary>
/// <param name="Status">generating, ready, approved or failed; a draft stuck generating after a restart reads as failed.</param>
/// <param name="Version">She approves this version; any edit makes a new one and clears her tick (D-92).</param>
/// <param name="Sent">Families have it: read only.</param>
/// <param name="Removed">Sections she removed: they are not sent.</param>
/// <param name="Pairs">The book's pairs she can choose for the picture question (D-92); <paramref name="PictureIndex"/> is the one in use.</param>
/// <param name="Edited">The Kannada is hers (D-92).</param>
/// <param name="Note">Her note per language, or null.</param>
public sealed record TodaysMessageView(
    DateOnly Date,
    string Status,
    int Version,
    int? ApprovedVersion,
    DateTimeOffset? ApprovedAt,
    bool Sent,
    string? Verdict,
    IReadOnlyList<string> CompetencyIds,
    TodaysPicture? Picture,
    TodaysVideo? Video,
    TodaysNote? Note,
    IReadOnlyList<string> Removed,
    IReadOnlyList<TodaysLanguageView> Languages,
    IReadOnlyList<IReadOnlyList<int>> Pairs,
    int? PictureIndex,
    bool Edited);

/// <summary>Why her edit was not saved (D-92): the check's code, what exactly was wrong, and the numbers she may use.</summary>
public sealed record TodaysEditProblem(string Problem, string? Detail, IReadOnlyList<string> Allowed);

/// <summary>
/// Today's message (D-91, T-88): drafted when she confirms the lesson, changed by her, approved at one version, and sent
/// with the evening message. Nothing here sends to a family except through <see cref="EveningSender"/>, which attaches
/// only an approved, current version (guardrail 3); in the demo Approve runs that send at once (D-94).
/// </summary>
public sealed class TodaysMessageService(
    ITodaysMessageStore store,
    IEveningStore evening,
    EveningSender sender,
    CurriculumGraph curriculum,
    TodaysMessageQueue queue,
    DemoSendOnApprove sendOnApprove,
    TimeProvider clock)
{
    /// <summary>
    /// Starts the draft for her confirmed lesson when it has the book's exercises (D-93) and someone is due tonight's
    /// message; a replay of the same lesson changes nothing. <paramref name="force"/>: "Try again".
    /// </summary>
    public async Task<bool> StartAsync(
        Guid classId, DateOnly date, IReadOnlyList<string> competencyIds, bool force, CancellationToken cancellationToken)
    {
        var lessons = competencyIds.Select(curriculum.Get).OfType<Competency>().ToList();
        if (!TodaysPicturePicker.Supported(lessons) || await LanguagesAsync(classId, date, cancellationToken) is not { Count: > 0 } families)
        {
            return false;
        }

        var existing = await store.GetAsync(classId, date, cancellationToken);
        if (!force && existing is not null && existing.CompetencyIds.SequenceEqual(competencyIds) && existing.Status != TodaysMessageStatus.Failed)
        {
            return false;
        }

        if (await store.StartAsync(classId, date, competencyIds, clock.GetUtcNow(), cancellationToken) is not { } id)
        {
            return false;
        }

        // Kannada first: it is hers to read and approve (D-92).
        var languages = families.Keys.Append("kn").Distinct().OrderBy(l => l == "kn" ? 0 : 1).ThenBy(l => l).ToList();
        queue.TryEnqueue(new TodaysJob(id, languages));
        return true;
    }

    /// <summary>"Try again" (or a first draft she asks for): from the day's confirmed lesson.</summary>
    public async Task<bool> RetryAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        await evening.LatestConfirmedLessonAsync(classId, date, cancellationToken) is { } lesson
        && lesson.Date == date
        && await StartAsync(classId, date, lesson.CompetencyIds, force: true, cancellationToken);

    public async Task<TodaysMessageView?> GetAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        if (await store.GetAsync(classId, date, cancellationToken) is not { } row)
        {
            return null;
        }

        var families = await LanguagesAsync(classId, date, cancellationToken) ?? new Dictionary<string, int>();
        var stale = row.Status == TodaysMessageStatus.Generating && clock.GetUtcNow() - row.UpdatedAt > CatchUpEndpoints.StaleGenerating;
        var sections = row.Sections;
        var languages = sections is null
            ? []
            : sections.Languages.Select(l => new TodaysLanguageView(
                l,
                families.GetValueOrDefault(l),
                sections.Concept.Text[l],
                sections.Concept.Clips.TryGetValue(l, out var clip) ? $"/api/audio/{clip}" : null,
                sections.Activity.Steps.GetValueOrDefault(l) ?? [])).ToList();
        var removed = sections is null
            ? []
            : TodaysSectionKind.All.Where(k => IsRemoved(sections, k)).ToList();
        var pairs = Pairs(row.CompetencyIds);
        var index = sections is null ? -1 : pairs.FindIndex(p => p[0] == sections.Picture.A && p[1] == sections.Picture.B);
        return new TodaysMessageView(
            row.Date,
            stale ? TodaysMessageStatus.Failed : row.Status,
            row.Version,
            row.ApprovedVersion,
            row.ApprovedAt,
            row.Sent,
            stale ? "error:stale" : row.Verdict,
            row.CompetencyIds,
            sections?.Picture,
            sections?.Video,
            sections?.Note,
            removed,
            languages,
            pairs,
            index < 0 ? null : index,
            sections?.Teacher ?? false);
    }

    /// <summary>
    /// Her Kannada for one section (D-92). Her text goes through the same checks as A9's (names, numbers, script, length);
    /// on a problem nothing is saved. Otherwise the message goes back to generating: the other languages are written from
    /// her Kannada and every concept is voiced again, then it is ready at the next version and she ticks again.
    /// </summary>
    public async Task<(TodaysChange Change, TodaysEditProblem? Problem)> EditAsync(
        Guid classId,
        DateOnly date,
        Guid clientRequestId,
        int version,
        string kind,
        string? text,
        IReadOnlyList<string>? steps,
        CancellationToken cancellationToken)
    {
        if (await store.GetAsync(classId, date, cancellationToken) is not { } row)
        {
            return (TodaysChange.NotFound, null);
        }

        if (row.Sections is not { } sections || row.Version != version)
        {
            return (TodaysChange.Conflict, null);
        }

        var cleaned = steps?.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (sections.WithKannada(kind, text?.Trim(), cleaned) is not { } edited)
        {
            return (TodaysChange.Conflict, null);
        }

        var lessons = row.CompetencyIds.Select(curriculum.Get).OfType<Competency>().ToList();
        var allowed = TodaysMessageValidator.AllowedNumbers(lessons, edited.Picture);
        var names = (await evening.GetClassAsync(classId, cancellationToken))?.Children.Select(c => c.FirstName).ToList() ?? [];
        var kannada = edited.Kannada;
        if (TodaysMessageValidator.Check("kn", new TodaysText(kannada.Concept, kannada.Steps, kannada.Note), allowed, names) is { } refused)
        {
            // Never echo a name back: the detail only says what kind of problem it was.
            return (TodaysChange.Conflict, new TodaysEditProblem(
                refused.Verdict.Replace("fallback:", ""), refused.Detail, allowed.OrderBy(n => n.Length).ThenBy(n => n, StringComparer.Ordinal).ToList()));
        }

        var change = await store.ReviseAsync(classId, date, clientRequestId, version, _ => edited, redraft: true, clock.GetUtcNow(), cancellationToken);
        if (change == TodaysChange.Applied)
        {
            queue.TryEnqueue(new TodaysJob(row.Id, edited.Languages));
        }

        return (change, null);
    }

    /// <summary>Another pair from the book's list for the picture question (D-92); code recomputes the answer. No AI.</summary>
    public async Task<TodaysChange> SetPictureAsync(
        Guid classId, DateOnly date, Guid clientRequestId, int version, int index, CancellationToken cancellationToken)
    {
        if (await store.GetAsync(classId, date, cancellationToken) is not { } row)
        {
            return TodaysChange.NotFound;
        }

        var lessons = row.CompetencyIds.Select(curriculum.Get).OfType<Competency>().ToList();
        if (index < 0 || index >= Pairs(row.CompetencyIds).Count || TodaysPicturePicker.Pick(lessons, index) is not { } picture)
        {
            return TodaysChange.Conflict;
        }

        return await store.ReviseAsync(
            classId, date, clientRequestId, version,
            s => s with { Picture = picture with { Removed = s.Picture.Removed } },
            redraft: false, clock.GetUtcNow(), cancellationToken);
    }

    private List<IReadOnlyList<int>> Pairs(IReadOnlyList<string> competencyIds) =>
        competencyIds.Select(curriculum.Get).OfType<Competency>()
            .Select(l => l.Exercises?.Compare).FirstOrDefault(c => c is { Count: > 0 })?.ToList() ?? [];

    /// <summary>Approves the version she read; in the demo it then goes at once (D-94), elsewhere at 6 pm.</summary>
    public async Task<TodaysChange> ApproveAsync(
        Guid classId, DateOnly date, Guid clientRequestId, int version, Guid teacherId, CancellationToken cancellationToken)
    {
        var result = await store.ApproveAsync(classId, date, clientRequestId, version, teacherId, clock.GetUtcNow(), cancellationToken);
        if (result == TodaysChange.Applied)
        {
            // The evening send attaches it only if it is still approved at this version (guardrail 3).
            await sendOnApprove.RunAsync(ct => sender.SendAsync(classId, date, ct), cancellationToken);
        }

        return result;
    }

    public Task<TodaysChange> SetRemovedAsync(
        Guid classId, DateOnly date, Guid clientRequestId, string kind, bool removed, CancellationToken cancellationToken) =>
        store.SetRemovedAsync(classId, date, clientRequestId, kind, removed, clock.GetUtcNow(), cancellationToken);

    /// <summary>How many families get tonight's message, by language; null when the class isn't there.</summary>
    private async Task<Dictionary<string, int>?> LanguagesAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        if (await sender.PlanAsync(classId, date, cancellationToken) is not { } plan
            || await evening.GetClassAsync(classId, cancellationToken) is not { } group)
        {
            return null;
        }

        var due = plan.Children
            .Where(c => c.Outcome is EveningOutcome.WillSend or EveningOutcome.Sent)
            .Select(c => c.ChildId)
            .ToHashSet();
        return group.Children.Where(c => due.Contains(c.Id)).GroupBy(c => c.HomeLanguage).ToDictionary(g => g.Key, g => g.Count());
    }

    private static bool IsRemoved(TodaysSections sections, string kind) => kind switch
    {
        TodaysSectionKind.Concept => sections.Concept.Removed,
        TodaysSectionKind.Activity => sections.Activity.Removed,
        TodaysSectionKind.Picture => sections.Picture.Removed,
        TodaysSectionKind.Video => sections.Video?.Removed == true,
        TodaysSectionKind.Note => sections.Note?.Removed == true,
        _ => false,
    };
}
