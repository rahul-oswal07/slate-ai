using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Demo;

public sealed record SatsFeedRequest(Guid ClassId, DateOnly Date, string Mode);

/// <summary>
/// Demo-shell controls (FR-14, D-69). Mapped only when <c>Demo:Enabled</c> is true (the default, for judges);
/// a real school deployment turns it off and the endpoints don't exist.
/// </summary>
public static class DemoEndpoints
{
    public const string Waiting = "waiting";
    public const string Arrive = "arrive";

    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        if (!app.ServiceProvider.GetRequiredService<IOptions<DemoOptions>>().Value.Enabled)
        {
            return app;
        }

        app.MapPost("/api/demo/sats-feed", SatsFeedAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("Make the simulated SATS register late, or arrive now")
            .WithDescription(
                "mode=waiting removes the date's register and holds SATS back (Today shows \"not in yet\"); " +
                "mode=arrive releases it and stores the simulated register with receivedAt = now. Demo only.");
        return app;
    }

    private static async Task<Results<Ok<AttendanceDayResponse>, NotFound, ValidationProblem>> SatsFeedAsync(
        SatsFeedRequest request,
        HttpContext http,
        IAttendanceStore store,
        SimulatedSatsSource source,
        SatsDemoHolds holds,
        IOptions<SatsOptions> options,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.Mode is not (Waiting or Arrive))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["mode"] = [$"Use \"{Waiting}\" or \"{Arrive}\"."],
            });
        }

        if (http.User.GetClassId() != request.ClassId
            || await store.GetRosterAsync(request.ClassId, cancellationToken) is not { } roster)
        {
            return TypedResults.NotFound();
        }

        var now = clock.GetUtcNow();
        IReadOnlyList<AttendanceRow> rows = [];
        if (request.Mode == Waiting)
        {
            holds.Hold(request.ClassId, request.Date);
            await store.RemoveDayAsync(request.ClassId, request.Date, cancellationToken);
        }
        else
        {
            holds.Release(request.ClassId, request.Date);
            var register = new SourceResult.Arrived(source.Simulate(request.ClassId, request.Date, roster), now);
            rows = await store.SaveSourceRegisterAsync(
                request.ClassId, request.Date, roster.Select(c => c.Id).ToList(), register, cancellationToken);
        }

        return TypedResults.Ok(AttendanceDay.Describe(request.Date, roster, rows, false, now, options.Value));
    }
}
