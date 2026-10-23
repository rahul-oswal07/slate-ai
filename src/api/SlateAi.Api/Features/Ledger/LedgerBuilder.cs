using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps.Compression;

namespace SlateAi.Api.Features.Ledger;

/// <summary>
/// Pure ledger computation (FR-05): attendance × confirmed lessons, the catch-up status per missed lesson, the
/// family's channel, this week's history, absence spells and return speed. No AI.
/// </summary>
public static class LedgerBuilder
{
    /// <param name="today">The school day (IST) that "this week" ends on; defaults to the latest register day.</param>
    public static LedgerResponse Build(Guid classId, LedgerData data, DateOnly? today = null)
    {
        DateOnly? latest = data.Register.Count > 0 ? data.Register.Max(r => r.Date) : null;
        var asOf = today ?? latest ?? DateOnly.MinValue;
        var weekOf = CompressionPlanner.Week(asOf).From;
        bool ThisWeek(DateOnly date) => date >= weekOf && date <= asOf;

        var register = data.Register.ToLookup(r => r.ChildId);
        var lessons = data.Lessons.OrderBy(l => l.Date).ToLookup(l => l.Date);
        var catchUps = (data.CatchUps ?? []).ToLookup(c => c.ChildId);
        var reasons = (data.Reasons ?? []).ToLookup(r => r.ChildId);

        var allSpells = new List<AbsenceSpell>();
        var rows = data.Children.OrderBy(c => c.RollNo).Select(child =>
        {
            var days = register[child.Id].ToList();
            var spells = AbsenceSpells.For(days.Select(d => (d.Date, d.Present)));
            allSpells.AddRange(spells);
            var away = days.Where(d => !d.Present).OrderBy(d => d.Date).ToList();
            var missed = away
                .SelectMany(d => lessons[d.Date])
                .Select(l => new MissedLesson(l.Id, l.Date, l.CompetencyIds, Status(l, catchUps[child.Id])))
                .ToList();
            return new LedgerChild(
                child.Id,
                child.RollNo,
                child.FirstName,
                AbsenceSpells.CurrentDays(spells),
                missed,
                spells,
                child.HomeLanguage,
                Channel(child),
                away.Count(d => ThisWeek(d.Date)),
                History(child, away.Where(d => ThisWeek(d.Date)), lessons, catchUps[child.Id], reasons[child.Id], ThisWeek));
        }).ToList();

        var returnSpeed = latest is { } day ? AbsenceSpells.Summarise(allSpells, day) : null;
        var weekLessons = data.Lessons.Where(l => ThisWeek(l.Date)).OrderBy(l => l.Date)
            .Select(l => new LedgerLesson(l.Id, l.Date, l.CompetencyIds)).ToList();
        return new LedgerResponse(classId, latest, returnSpeed, rows, weekOf, weekLessons);
    }

    /// <summary>
    /// Learning status only (B-08): the answer wins (it is what the child showed), then a sent link. The family's
    /// channel (stopped, no phone, no consent) is a separate field, so a stop never hides "caught up". A lesson
    /// belongs to the week's catch-up when one of its competencies is among that catch-up's missed ones.
    /// </summary>
    private static string Status(LessonRow lesson, IEnumerable<CatchUpRow> catchUps)
    {
        var catchUp = CatchUpFor(lesson, catchUps);
        return catchUp?.Outcome ?? (catchUp?.SentAt is not null ? LedgerStatus.Sent : LedgerStatus.Missed);
    }

    private static CatchUpRow? CatchUpFor(LessonRow lesson, IEnumerable<CatchUpRow> catchUps)
    {
        var week = CompressionPlanner.Week(lesson.Date).From;
        return catchUps.FirstOrDefault(c =>
            c.WeekOf == week && c.MissedCompetencyIds.Intersect(lesson.CompetencyIds, StringComparer.Ordinal).Any());
    }

    /// <summary>How the family can be reached (D-39, FR-19): a stop first, then what the consent form says.</summary>
    private static string? Channel(LedgerChildRow child) =>
        child.StoppedAt is not null ? LedgerChannel.Stopped
        : child.Consent == "nophone" ? LedgerChannel.NoPhone
        : child.Consent != "yes" ? LedgerChannel.NoConsent
        : null;

    /// <summary>This week's events for the child history (T4), by day; on one day in the order they happen.</summary>
    private static IReadOnlyList<LedgerEvent> History(
        LedgerChildRow child,
        IEnumerable<RegisterRow> awayThisWeek,
        ILookup<DateOnly, LessonRow> lessons,
        IEnumerable<CatchUpRow> catchUps,
        IEnumerable<ReasonRow> reasons,
        Func<DateOnly, bool> thisWeek)
    {
        var events = new List<(LedgerEvent Event, int Order)>();
        foreach (var day in awayThisWeek)
        {
            events.Add((new LedgerEvent(day.Date, LedgerEventKind.Away,
                CompetencyIds: lessons[day.Date].SelectMany(l => l.CompetencyIds).Distinct().ToList()), 0));
        }

        foreach (var reason in reasons.Where(r => thisWeek(r.Date)))
        {
            events.Add((new LedgerEvent(reason.Date, LedgerEventKind.Reason, Category: reason.Category), 1));
        }

        foreach (var catchUp in catchUps)
        {
            if (catchUp.SentAt is { } sent && thisWeek(SchoolClock.DateOf(sent)))
            {
                events.Add((new LedgerEvent(SchoolClock.DateOf(sent), LedgerEventKind.CatchUpSent, Language: catchUp.Language), 2));
            }

            if (catchUp is { Outcome: { } outcome, AnsweredAt: { } answered } && thisWeek(SchoolClock.DateOf(answered)))
            {
                events.Add((new LedgerEvent(SchoolClock.DateOf(answered), LedgerEventKind.Answered, Outcome: outcome), 3));
            }
        }

        if (child.StoppedAt is { } stopped && thisWeek(SchoolClock.DateOf(stopped)))
        {
            events.Add((new LedgerEvent(SchoolClock.DateOf(stopped), LedgerEventKind.Stopped), 4));
        }

        return events.OrderBy(e => e.Event.Date).ThenBy(e => e.Order).Select(e => e.Event).ToList();
    }
}
