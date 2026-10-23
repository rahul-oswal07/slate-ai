using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Ledger;

public sealed record LedgerResponse(
    Guid ClassId,
    DateOnly? LatestSchoolDay,
    ReturnSpeed? ReturnSpeed,
    IReadOnlyList<LedgerChild> Children);

public sealed record LedgerChild(
    Guid ChildId,
    int RollNo,
    string FirstName,
    int CurrentAbsenceDays,
    IReadOnlyList<MissedLesson> MissedLessons,
    IReadOnlyList<AbsenceSpell> Absences);

/// <param name="Status">See <see cref="LedgerStatus"/>.</param>
public sealed record MissedLesson(Guid LessonId, DateOnly Date, IReadOnlyList<string> CompetencyIds, string Status);

/// <summary>Where a missed lesson stands (FR-05, FR-11, FR-19), computed from attendance and catch-ups.</summary>
public static class LedgerStatus
{
    public const string Missed = "missed";

    /// <summary>The family got the catch-up link; no answer yet.</summary>
    public const string Sent = "sent";

    public const string CaughtUp = "caughtUp";
    public const string NeedsBuddy = "needsBuddy";

    /// <summary>The family stopped messages; the child stays on the buddy card (FR-19).</summary>
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
                "Computed from attendance × lessons on every request (FR-05). A missed lesson's status comes from the " +
                "child's catch-up for that week: caughtUp / needsBuddy once the picture check is answered (FR-11), " +
                "else stopped when the family stopped messages (FR-19), else sent, else missed. returnSpeed covers " +
                "absence spells that started in the 7 days up to the latest school day.");
        return app;
    }

    private static async Task<Results<Ok<LedgerResponse>, NotFound>> GetAsync(
        Guid classId, ILedgerStore store, CancellationToken cancellationToken)
    {
        var data = await store.LoadAsync(classId, cancellationToken);
        return data is null ? TypedResults.NotFound() : TypedResults.Ok(LedgerBuilder.Build(classId, data));
    }
}
