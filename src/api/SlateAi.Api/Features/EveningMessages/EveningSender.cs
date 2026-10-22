using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;

namespace SlateAi.Api.Features.EveningMessages;

public sealed record EveningChildView(Guid ChildId, string Outcome);

/// <summary>Tonight's evening messages for a class (T1 tonight's card; the send's result).</summary>
/// <param name="Blocked">Why nothing goes tonight (<see cref="EveningBlock"/>), or null.</param>
/// <param name="Absent">Children absent today.</param>
/// <param name="Families">Absent children whose family gets (or got) the message.</param>
/// <param name="Sent">Messages already sent today.</param>
/// <param name="SendsAt">When the evening job sends, IST.</param>
/// <param name="Children">Every absent child and what happens for them tonight.</param>
public sealed record EveningDayResponse(
    DateOnly Date,
    string? Blocked,
    bool TemplateApproved,
    bool LessonConfirmed,
    int Absent,
    int Families,
    int Sent,
    TimeOnly SendsAt,
    IReadOnlyList<EveningChildView> Children);

/// <summary>
/// Works out and sends the evening message on a day of absence (FR-20). Code only, no LLM: the class's approved
/// template, slots from her confirmed lesson, and only families with consent Yes who haven't stopped (guardrail 3).
/// </summary>
public sealed class EveningSender(
    IEveningStore store,
    AttendanceDayLoader attendance,
    CurriculumGraph curriculum,
    EveningWording wording,
    IOptions<EveningOptions> options,
    TimeProvider clock,
    ILogger<EveningSender> logger)
{
    /// <summary>What would happen tonight, without sending. Null if the class doesn't exist.</summary>
    public async Task<EveningDayResponse?> PlanAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        (await BuildAsync(classId, date, cancellationToken))?.Day;

    /// <summary>Sends to every family due a message and not yet sent one today. Safe to repeat.</summary>
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
        return plan.Day with { Children = children, Sent = children.Count(c => c.Outcome == EveningOutcome.Sent) };
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
        var templateApproved = templates.Count > 0 && templates.Values.All(t => t.Status == TemplateStatus.Approved);
        var lesson = await store.LatestConfirmedLessonAsync(classId, date, cancellationToken);
        var lessonConfirmed = lesson?.Date == date;
        var attendanceIn = register.Status is AttendanceDayStatus.Sats or AttendanceDayStatus.Manual;
        var blocked = EveningEligibility.Block(templateApproved, lessonConfirmed, attendanceIn);

        var sent = await store.GetSentChildIdsAsync(classId, date, cancellationToken);
        var absent = register.AbsentChildIds.ToHashSet();
        var now = clock.GetUtcNow();
        var children = new List<EveningChildView>();
        var pending = new List<NewEveningMessage>();
        foreach (var child in group.Children.Where(c => absent.Contains(c.Id)))
        {
            var template = templates.GetValueOrDefault(child.HomeLanguage);
            var filled = template is null || !lessonConfirmed
                ? null
                : EveningSlots.Fill(template.Wording, child.HomeLanguage, child.FirstName, lesson!.CompetencyIds, curriculum);
            // Before the lesson is confirmed only the template can be checked; the names are checked once it is.
            var hasWording = template is not null && (filled is not null || !lessonConfirmed);
            var outcome = EveningEligibility.Decide(child.Consent, child.Stopped, sent.Contains(child.Id), hasWording);
            children.Add(new EveningChildView(child.Id, outcome));
            if (outcome == EveningOutcome.WillSend && filled is not null && blocked is null)
            {
                pending.Add(new NewEveningMessage(
                    child.Id, template!.Id, lesson!.Id, child.HomeLanguage, filled.Slots, NewToken(), now,
                    now.AddDays(options.Value.LinkDays)));
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
            children);
        return new Plan(day, pending);
    }

    /// <summary>128 random bits, URL-safe: the only key to the parent view.</summary>
    private static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));

    private sealed record Plan(EveningDayResponse Day, IReadOnlyList<NewEveningMessage> Pending);
}
