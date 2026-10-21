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

/// <summary>Status is "missed" until catch-up sending (T-14) and picture scoring (T-15) exist.</summary>
public sealed record MissedLesson(Guid LessonId, DateOnly Date, IReadOnlyList<string> CompetencyIds, string Status);

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
                "Computed from attendance × lessons on every request (FR-05). returnSpeed covers absence spells " +
                "that started in the 7 days up to the latest school day.");
        return app;
    }

    private static async Task<Results<Ok<LedgerResponse>, NotFound>> GetAsync(
        Guid classId, ILedgerStore store, CancellationToken cancellationToken)
    {
        var data = await store.LoadAsync(classId, cancellationToken);
        return data is null ? TypedResults.NotFound() : TypedResults.Ok(LedgerBuilder.Build(classId, data));
    }
}
