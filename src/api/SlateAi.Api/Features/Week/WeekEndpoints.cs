using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Stars;

namespace SlateAi.Api.Features.Week;

/// <param name="Decision">"recap" (plan a 10-minute recap; Today reminds her next week) or "skip".</param>
/// <param name="LessonId">The nudge's lesson, so a decision can't land on a different lesson than she saw.</param>
public sealed record PacingDecisionRequest(Guid ClientRequestId, string Decision, Guid LessonId);

/// <summary>
/// T6 Week (FR-22, FR-21, FR-13; D-20, D-24, D-60, D-61): return speed for this week and last, the families'
/// reasons, and the pacing nudge with her decision. All numbers are counted in code from attendance; no AI. Teacher
/// only, never exported (FR-22).
/// </summary>
public static class WeekEndpoints
{
    public static IServiceCollection AddWeek(this IServiceCollection services) =>
        services.AddScoped<IWeekStore, WeekStore>();

    public static IEndpointRouteBuilder MapWeekEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/classes/{classId:guid}/week/{date}", GetAsync)
            .RequireClassOwner()
            .WithTags("Week")
            .WithSummary("The school week (Mon–Sat) containing a date: return speed, reasons, pacing nudge (T6)")
            .WithDescription(
                "A spell belongs to the week it started in; a child still away counts in the total but not as back " +
                "within 2 school days. Reasons: one per child absent that week (latest), else notGiven. Nudge when 5 " +
                "or more children missed the same lesson (D-60). empty = no absence started that week (D-61). " +
                "Future weeks are 400. stars (FR-24): who earned the week's star, or is on track in the current week; null when " +
                "the week has no register.");

        app.MapPut("/api/classes/{classId:guid}/week/{date}/pacing", DecideAsync)
            .RequireClassOwner()
            .WithTags("Week")
            .WithSummary("Plan a recap or skip it this week (T6 nudge)")
            .WithDescription(
                "Change = send again. Replaying the same clientRequestId changes nothing. 409 when the week has no " +
                "nudge or lessonId isn't the nudge's lesson (the week changed since she looked). Returns the week.");
        return app;
    }

    private static async Task<Results<Ok<WeekResponse>, NotFound, ValidationProblem>> GetAsync(
        Guid classId,
        DateOnly date,
        ILedgerStore ledger,
        IWeekStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        IOptions<StarsOptions> stars,
        CancellationToken cancellationToken)
    {
        var weekOf = CompressionPlanner.Week(date).From;
        if (weekOf > SchoolClock.Today(clock))
        {
            return FutureWeek();
        }

        return await WeekAsync(classId, weekOf, ledger, store, curriculum, clock, stars.Value, cancellationToken) is { } week
            ? TypedResults.Ok(week)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<WeekResponse>, Conflict<WeekResponse>, NotFound, ValidationProblem>> DecideAsync(
        Guid classId,
        DateOnly date,
        PacingDecisionRequest request,
        ILedgerStore ledger,
        IWeekStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        IOptions<StarsOptions> stars,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty || request.Decision is not ("recap" or "skip"))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [request.ClientRequestId == Guid.Empty ? "clientRequestId" : "decision"] =
                    [request.ClientRequestId == Guid.Empty ? "Required." : "Must be recap or skip."],
            });
        }

        var weekOf = CompressionPlanner.Week(date).From;
        if (weekOf > SchoolClock.Today(clock))
        {
            return FutureWeek();
        }

        if (await WeekAsync(classId, weekOf, ledger, store, curriculum, clock, stars.Value, cancellationToken) is not { } week)
        {
            return TypedResults.NotFound();
        }

        if (week.Nudge?.LessonId != request.LessonId)
        {
            return TypedResults.Conflict(week);
        }

        await store.SaveDecisionAsync(
            classId, weekOf, new DecisionRow(request.LessonId, request.Decision), request.ClientRequestId, clock.GetUtcNow(),
            cancellationToken);
        return TypedResults.Ok((await WeekAsync(classId, weekOf, ledger, store, curriculum, clock, stars.Value, cancellationToken))!);
    }

    private static async Task<WeekResponse?> WeekAsync(
        Guid classId, DateOnly weekOf, ILedgerStore ledger, IWeekStore store, CurriculumGraph curriculum,
        TimeProvider clock, StarsOptions stars, CancellationToken cancellationToken)
    {
        if (await ledger.LoadAsync(classId, cancellationToken) is not { } data)
        {
            return null;
        }

        var decision = await store.GetDecisionAsync(classId, weekOf, cancellationToken);
        return WeekBuilder.Build(data, curriculum, weekOf, decision)
            with { Stars = StarRules.For(data, weekOf, SchoolClock.Today(clock), stars) };
    }

    private static ValidationProblem FutureWeek() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Can't use a future week."] });
}
