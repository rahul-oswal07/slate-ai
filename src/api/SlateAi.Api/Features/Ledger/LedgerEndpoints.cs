using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Ledger;

/// <param name="WeekOf">The Monday of this school week; "this week" runs from it to today (IST).</param>
/// <param name="Lessons">This week's confirmed lessons, oldest first (T4 "By lesson").</param>
public sealed record LedgerResponse(
    Guid ClassId,
    DateOnly? LatestSchoolDay,
    ReturnSpeed? ReturnSpeed,
    IReadOnlyList<LedgerChild> Children,
    DateOnly WeekOf,
    IReadOnlyList<LedgerLesson> Lessons);

/// <param name="Channel">How the family can be reached, separate from the learning status (B-08): see <see cref="LedgerChannel"/>; null when messages reach them.</param>
/// <param name="DaysAwayThisWeek">School days absent this week.</param>
/// <param name="History">This week's events for the child history (T4), by day.</param>
public sealed record LedgerChild(
    Guid ChildId,
    int RollNo,
    string FirstName,
    int CurrentAbsenceDays,
    IReadOnlyList<MissedLesson> MissedLessons,
    IReadOnlyList<AbsenceSpell> Absences,
    string HomeLanguage,
    string? Channel,
    int DaysAwayThisWeek,
    IReadOnlyList<LedgerEvent> History);

/// <param name="Status">Learning status only: see <see cref="LedgerStatus"/>.</param>
public sealed record MissedLesson(Guid LessonId, DateOnly Date, IReadOnlyList<string> CompetencyIds, string Status);

public sealed record LedgerLesson(Guid LessonId, DateOnly Date, IReadOnlyList<string> CompetencyIds);

/// <summary>One line of the child history. Only the fields its <see cref="Kind"/> needs are set.</summary>
public sealed record LedgerEvent(
    DateOnly Date,
    string Kind,
    IReadOnlyList<string>? CompetencyIds = null,
    string? Category = null,
    string? Language = null,
    string? Outcome = null);

/// <summary>Where a missed lesson stands in learning terms (FR-05, FR-11), from attendance and catch-ups.</summary>
public static class LedgerStatus
{
    public const string Missed = "missed";

    /// <summary>The family got the catch-up link; no answer yet.</summary>
    public const string Sent = "sent";

    public const string CaughtUp = "caughtUp";
    public const string NeedsBuddy = "needsBuddy";
}

/// <summary>How the family can be reached (D-39, FR-19): a separate tag from the learning status (B-08).</summary>
public static class LedgerChannel
{
    /// <summary>The family stopped messages; the child stays on the buddy card (FR-19).</summary>
    public const string Stopped = "stopped";

    public const string NoPhone = "noPhone";
    public const string NoConsent = "noConsent";
}

public static class LedgerEventKind
{
    /// <summary>Absent, with the lessons confirmed that day.</summary>
    public const string Away = "away";

    /// <summary>The family's reason for that day (FR-21), shown "from the parent".</summary>
    public const string Reason = "reason";

    public const string CatchUpSent = "catchUpSent";
    public const string Answered = "answered";
    public const string Stopped = "stopped";
}

public static class LedgerEndpoints
{
    public static IServiceCollection AddLedger(this IServiceCollection services) =>
        services.AddScoped<ILedgerStore, LedgerStore>();

    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/classes/{classId:guid}/ledger", GetAsync)
            .RequireClassOwner()
            .WithTags("Ledger")
            .WithSummary("Missed lessons and absence spells per child")
            .WithDescription(
                "Computed from attendance × confirmed lessons on every request (FR-05); no AI. A missed lesson's " +
                "status is learning only, from the child's catch-up for that week: caughtUp / needsBuddy once the " +
                "picture check is answered (FR-11), else sent, else missed. channel is separate (B-08): stopped " +
                "(FR-19), noPhone, noConsent or null. weekOf / lessons / daysAwayThisWeek / history cover this " +
                "school week up to today (IST). returnSpeed covers absence spells that started in the 7 days up to " +
                "the latest school day.");
        return app;
    }

    private static async Task<Results<Ok<LedgerResponse>, NotFound>> GetAsync(
        Guid classId, ILedgerStore store, TimeProvider clock, CancellationToken cancellationToken)
    {
        var data = await store.LoadAsync(classId, cancellationToken);
        return data is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(LedgerBuilder.Build(classId, data, SchoolClock.Today(clock)));
    }
}
