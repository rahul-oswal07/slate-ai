using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;

namespace SlateAi.Api.Features.EveningMessages;

/// <param name="Language">The family's language: what their message is written in.</param>
/// <param name="WithTodays">Today's message goes (or went) with this family's message; false: the text only.</param>
public sealed record EveningChildView(Guid ChildId, string Outcome, string Language, bool WithTodays);

/// <summary>
/// One family's real evening message as Today's Preview shows it (T-86): the text with the child's name and the lesson
/// filled by code, and whether today's message goes with it. Read only: nothing is sent and no link is made.
/// </summary>
public sealed record EveningPreviewResponse(
    Guid ChildId, string Language, string Text, bool WithTodays, bool Sent, DateTimeOffset? SentAt);

/// <summary>Tonight's evening messages for a class (T1 tonight's card; the send's result).</summary>
/// <param name="Blocked">Why nothing goes tonight (<see cref="EveningBlock"/>), or null.</param>
/// <param name="Absent">Children absent today.</param>
/// <param name="Families">Absent children whose family gets (or got) the message.</param>
/// <param name="Sent">Messages already sent today.</param>
/// <param name="SendsAt">When the evening job sends, IST.</param>
/// <param name="Children">Every absent child and what happens for them tonight.</param>
/// <param name="GoesAt">
/// When the job sends if she does nothing (T-86, <see cref="EveningSchedule"/>): the send time, or later while it waits for
/// her to approve today's message. Null when nothing is due to go, on another day, or while today's message is written.
/// </param>
/// <param name="Waiting">What the job waits for (<see cref="EveningWaiting"/>), or null.</param>
/// <param name="SentAt">When the first message went that day, or null.</param>
/// <param name="WithTodays">Today's message goes (approved at its version) or went with the messages.</param>
public sealed record EveningDayResponse(
    DateOnly Date,
    string? Blocked,
    bool TemplateApproved,
    bool LessonConfirmed,
    int Absent,
    int Families,
    int Sent,
    TimeOnly SendsAt,
    IReadOnlyList<EveningChildView> Children,
    DateTimeOffset? GoesAt = null,
    string? Waiting = null,
    DateTimeOffset? SentAt = null,
    bool WithTodays = false);

/// <summary>
/// Works out and sends the evening message on a day of absence (FR-20). Code only, no LLM: the class's approved
/// template, slots from her confirmed lesson, and only families with consent Yes who haven't stopped (guardrail 3).
/// </summary>
public sealed class EveningSender(
    IEveningStore store,
    AttendanceDayLoader attendance,
    ICurriculumProvider curricula,
    EveningWording wording,
    IOptions<EveningOptions> options,
    TimeProvider clock,
    ILogger<EveningSender> logger)
{
    /// <summary>What would happen tonight, without sending. Null if the class doesn't exist.</summary>
    public async Task<EveningDayResponse?> PlanAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        (await BuildAsync(classId, date, cancellationToken))?.Day;

    /// <summary>
    /// One absent family's message as it goes (or went) that day, for her preview (T-86); null for a child who gets none.
    /// </summary>
    public async Task<EveningPreviewResponse?> PreviewAsync(
        Guid classId, DateOnly date, Guid childId, CancellationToken cancellationToken) =>
        (await BuildAsync(classId, date, cancellationToken))?.Previews.GetValueOrDefault(childId);

    /// <summary>Sends to every family due a message and not yet sent one that day. Safe to repeat.</summary>
    public async Task<EveningDayResponse?> SendAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        if (await BuildAsync(classId, date, cancellationToken) is not { } plan)
        {
            return null;
        }

        if (plan.Day.Blocked is not null || plan.Pending.Count == 0)
        {
            return plan.Day;
        }

        var added = await store.AddMessagesAsync(classId, date, plan.Pending, cancellationToken);
        logger.LogInformation(
            "Evening messages for class {ClassId} on {Date}: {Added} sent", classId, date, added);

        var children = plan.Day.Children
            .Select(c => c.Outcome == EveningOutcome.WillSend ? c with { Outcome = EveningOutcome.Sent } : c)
            .ToList();
        return plan.Day with
        {
            Children = children,
            Sent = children.Count(c => c.Outcome == EveningOutcome.Sent),
            SentAt = plan.Day.SentAt ?? plan.Pending.Min(p => p.SentAt),
            WithTodays = children.Any(c => c.Outcome == EveningOutcome.Sent && c.WithTodays),
        };
    }

    private async Task<Plan?> BuildAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        if (await store.GetClassAsync(classId, cancellationToken) is not { } group
            || await attendance.LoadAsync(classId, date, cancellationToken) is not { } register)
        {
            return null;
        }

        var templates = (await store.GetTemplatesAsync(classId, wording, cancellationToken))
            .ToDictionary(t => t.Language);
        var curriculum = await curricula.ForClassAsync(classId, cancellationToken);
        var templateApproved = templates.Count > 0 && templates.Values.All(t => t.Status == TemplateStatus.Approved);
        var lesson = await store.LatestConfirmedLessonAsync(classId, date, cancellationToken);
        var lessonConfirmed = lesson?.Date == date;
        var attendanceIn = register.Status is AttendanceDayStatus.Sats or AttendanceDayStatus.Manual;
        var blocked = EveningEligibility.Block(templateApproved, lessonConfirmed, attendanceIn);

        // Only a clip she approved goes with the message; no clip, or one for another language, means the plain message.
        var lessonAudio = await store.GetApprovedLessonAudioAsync(classId, date, cancellationToken);
        // Today's message (D-91) goes only when approved at its current version; its concept replaces the lesson clip.
        var todays = await store.GetApprovedTodaysMessageAsync(classId, date, cancellationToken);
        var sent = (await store.GetSentAsync(classId, date, cancellationToken)).ToDictionary(m => m.ChildId);
        var absent = register.AbsentChildIds.ToHashSet();
        var now = clock.GetUtcNow();
        var children = new List<EveningChildView>();
        var pending = new List<NewEveningMessage>();
        var previews = new Dictionary<Guid, EveningPreviewResponse>();
        foreach (var child in group.Children.Where(c => absent.Contains(c.Id)))
        {
            var template = templates.GetValueOrDefault(child.HomeLanguage);
            var filled = template is null || !lessonConfirmed
                ? null
                : EveningSlots.Fill(
                    wording.WithBooks(template.Wording, child.HomeLanguage), child.HomeLanguage, child.FirstName,
                    lesson!.CompetencyIds, curriculum);
            // Before the lesson is confirmed only the template can be checked; the names are checked once it is.
            var hasWording = template is not null && (filled is not null || !lessonConfirmed);
            var outcome = EveningEligibility.Decide(child.Consent, child.Stopped, sent.ContainsKey(child.Id), hasWording);
            var withTodays = todays is { } t && t.Languages.Contains(child.HomeLanguage) ? t.Id : (Guid?)null;
            if (sent.GetValueOrDefault(child.Id) is { } went)
            {
                // What the family got: the wording and slots it was sent with, not today's.
                children.Add(new EveningChildView(child.Id, outcome, went.Language, went.WithTodays));
                previews[child.Id] = new EveningPreviewResponse(
                    child.Id, went.Language, EveningSlots.Render(went.TemplateText, child.FirstName, went.Slots).Text,
                    went.WithTodays, Sent: true, went.SentAt);
                continue;
            }

            var goes = outcome == EveningOutcome.WillSend;
            children.Add(new EveningChildView(child.Id, outcome, child.HomeLanguage, goes && withTodays is not null));
            if (goes && filled is not null)
            {
                previews[child.Id] = new EveningPreviewResponse(
                    child.Id, child.HomeLanguage, filled.Text, withTodays is not null, Sent: false, SentAt: null);
            }

            if (goes && filled is not null && blocked is null)
            {
                pending.Add(new NewEveningMessage(
                    child.Id, template!.Id, lesson!.Id, child.HomeLanguage, filled.Slots, NewToken(), now,
                    SchoolClock.RealNow(clock).AddDays(options.Value.LinkDays), // real, even on a chosen demo day (T-134)
                    withTodays is null && lessonAudio.TryGetValue(child.HomeLanguage, out var audioId) ? audioId : null,
                    withTodays));
            }
        }

        var refused = children.Where(c => c.Outcome == EveningOutcome.NoWording).ToList();
        if (refused.Count > 0)
        {
            // IDs only (NFR-05). A missing template or lesson name in a family's language is fixed in the data files.
            logger.LogWarning(
                "Evening message for class {ClassId} on {Date}: no wording for {Count} children ({ChildIds})",
                classId, date, refused.Count, string.Join(",", refused.Select(c => c.ChildId)));
        }

        var day = new EveningDayResponse(
            date,
            blocked,
            templateApproved,
            lessonConfirmed,
            children.Count,
            children.Count(c => c.Outcome is EveningOutcome.WillSend or EveningOutcome.Sent),
            children.Count(c => c.Outcome == EveningOutcome.Sent),
            options.Value.SendAt,
            children,
            SentAt: sent.Count > 0 ? sent.Values.Min(m => m.SentAt) : null,
            WithTodays: sent.Count > 0 ? sent.Values.Any(m => m.WithTodays) : todays is not null);
        return new Plan(day, pending, previews);
    }

    /// <summary>128 random bits, URL-safe: the only key to the parent view.</summary>
    private static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));

    private sealed record Plan(
        EveningDayResponse Day, IReadOnlyList<NewEveningMessage> Pending, IReadOnlyDictionary<Guid, EveningPreviewResponse> Previews);
}
