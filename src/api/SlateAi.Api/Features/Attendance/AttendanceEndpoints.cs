using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Attendance;

/// <summary>The full register for one school day: listed children are absent, everyone else present (FR-01).</summary>
public sealed record MarkAttendanceRequest(Guid ClientRequestId, DateOnly Date, IReadOnlyList<Guid> AbsentChildIds);

public sealed record MarkAttendanceResponse(DateOnly Date, int Present, int Absent);

public static class AttendanceEndpoints
{
    public static IServiceCollection AddAttendance(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SatsOptions>(configuration.GetSection(SatsOptions.Section));
        services.AddScoped<IAttendanceStore, AttendanceStore>();
        services.AddSingleton<SimulatedSatsSource>();
        services.AddSingleton<IAttendanceSource>(sp => sp.GetRequiredService<SimulatedSatsSource>());
        services.AddSingleton<SatsDemoHolds>();
        return services;
    }

    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/classes/{classId:guid}/attendance/{date}", GetDayAsync)
            .RequireClassOwner()
            .WithTags("Attendance")
            .WithSummary("The register for a date, with its source")
            .WithDescription(
                "Fetches the simulated SATS register when it is due (after Attendance:Sats:ArrivesAt IST) and " +
                "nothing from SATS is stored yet (D-45, D-69). status: sats | manual | waiting | missing (after the " +
                "Attendance:Sats:RetryAt retry, D-67) | noSchool.");

        app.MapPost("/api/classes/{classId:guid}/attendance", MarkAsync)
            .RequireClassOwner()
            .WithTags("Attendance")
            .WithSummary("Correct or mark a day's register")
            .WithDescription(
                "Over a SATS register this is a correction (never written back to SATS); with nothing stored it " +
                "is the teacher's own marking. Replaying the same clientRequestId is a no-op (offline outbox).");
        return app;
    }

    private static async Task<Results<Ok<AttendanceDayResponse>, NotFound, ValidationProblem>> GetDayAsync(
        Guid classId,
        DateOnly date,
        IAttendanceStore store,
        IAttendanceSource source,
        SatsDemoHolds holds,
        IOptions<SatsOptions> options,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (date > SchoolClock.Today(clock))
        {
            return FutureDate();
        }

        var roster = await store.GetRosterAsync(classId, cancellationToken);
        if (roster is null)
        {
            return TypedResults.NotFound();
        }

        var now = clock.GetUtcNow();
        var rows = await store.GetDayAsync(classId, date, cancellationToken);
        var noSchool = false;
        var satsDue = now >= SchoolClock.At(date, options.Value.ArrivesAt) && !holds.IsHeld(classId, date);
        if (satsDue && rows.All(r => r.SourcePresent is null))
        {
            switch (await source.FetchAsync(classId, date, roster, cancellationToken))
            {
                case SourceResult.Arrived register:
                    rows = await store.SaveSourceRegisterAsync(
                        classId, date, roster.Select(c => c.Id).ToList(), register, cancellationToken);
                    break;
                case SourceResult.NoSchool:
                    noSchool = true;
                    break;
            }
        }

        return TypedResults.Ok(AttendanceDay.Describe(date, roster, rows, noSchool, now, options.Value));
    }

    private static async Task<Results<Ok<MarkAttendanceResponse>, NotFound, ValidationProblem>> MarkAsync(
        Guid classId,
        MarkAttendanceRequest request,
        IAttendanceStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        if (request.Date > SchoolClock.Today(clock))
        {
            return FutureDate();
        }

        var roster = await store.GetRosterAsync(classId, cancellationToken);
        if (roster is null)
        {
            return TypedResults.NotFound();
        }

        var childIds = roster.Select(c => c.Id).ToList();
        var absent = (request.AbsentChildIds ?? []).ToHashSet();
        if (!absent.IsSubsetOf(childIds))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["absentChildIds"] = ["Every child must belong to this class."],
            });
        }

        return TypedResults.Ok(await store.SaveRegisterAsync(
            classId, request.ClientRequestId, request.Date, childIds, absent, cancellationToken));
    }

    private static ValidationProblem FutureDate() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["date"] = ["Can't use a future date."],
        });
}
