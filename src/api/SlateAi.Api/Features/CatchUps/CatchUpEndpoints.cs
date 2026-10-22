using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Script;

namespace SlateAi.Api.Features.CatchUps;

public sealed record GenerateCatchUpsRequest(Guid ClientRequestId);

/// <summary>The week's catch-ups for the teacher (T3 reads this).</summary>
/// <param name="WeekOf">The Monday of the school week.</param>
public sealed record CatchUpWeekResponse(DateOnly WeekOf, IReadOnlyList<PackageView> Packages);

/// <summary>
/// The catch-up pipeline (FR-06–FR-09): A2 compression → grouping with code-made numbers → A3 words. The verifier
/// (T-10), audio (T-11) and approval (T-14) follow.
/// </summary>
public static class CatchUpEndpoints
{
    /// <summary>A package still "generating" after this (app restarted mid-way) reads as failed.</summary>
    public static readonly TimeSpan StaleGenerating = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddCatchUps(this IServiceCollection services)
    {
        services.AddScoped<Compressor>();
        services.AddScoped<ScriptWriter>();
        services.AddScoped<ICatchUpStore, CatchUpStore>();
        services.AddScoped<CatchUpGenerator>();
        services.AddScoped<CatchUpWriter>();
        services.AddSingleton<CatchUpQueue>();
        services.AddHostedService<CatchUpWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapCatchUpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/classes/{classId:guid}/catchups/generate", GenerateAsync)
            .RequireClassOwner()
            .WithTags("Catch-ups")
            .WithSummary("Build this week's catch-ups (A2 → group → A3)")
            .WithDescription(
                "For the school week up to today: A2 picks each absent child's targets, children are grouped into one " +
                "catch-up per (targets, home language) with numbers made by code, and A3 writes the words in the " +
                "background (202; packages go generating → ready or failed). Re-running keeps existing catch-ups and " +
                "adds children absent since. Nothing is sent: approval comes first (T-14).");

        app.MapGet("/api/classes/{classId:guid}/packages", ListAsync)
            .RequireClassOwner()
            .WithTags("Catch-ups")
            .WithSummary("The week's catch-ups with their children and A2's reasons")
            .WithDescription("week = any date in the week (default today). Includes the check's correct option for the teacher.");
        return app;
    }

    private static async Task<Results<Accepted<CatchUpWeekResponse>, NotFound, ValidationProblem>> GenerateAsync(
        Guid classId,
        GenerateCatchUpsRequest request,
        CatchUpGenerator generator,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        var today = SchoolClock.Today(clock);
        return await generator.GenerateAsync(classId, today, cancellationToken) is { } packages
            ? TypedResults.Accepted((string?)null, Week(today, packages, clock.GetUtcNow()))
            : TypedResults.NotFound();
    }

    private static async Task<Ok<CatchUpWeekResponse>> ListAsync(
        Guid classId, DateOnly? week, ICatchUpStore store, TimeProvider clock, CancellationToken cancellationToken)
    {
        var date = week ?? SchoolClock.Today(clock);
        var weekOf = CompressionPlanner.Week(date).From;
        return TypedResults.Ok(Week(date, await store.ListAsync(classId, weekOf, cancellationToken), clock.GetUtcNow()));
    }

    private static CatchUpWeekResponse Week(DateOnly date, IReadOnlyList<PackageView> packages, DateTimeOffset now) =>
        new(CompressionPlanner.Week(date).From, packages
            .Select(p => p.Status == PackageStatus.Generating && now - p.UpdatedAt > StaleGenerating
                ? p with { Status = PackageStatus.Failed, Verdict = p.Verdict ?? "error:stale" }
                : p)
            .ToList());
}
