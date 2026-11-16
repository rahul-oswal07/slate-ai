using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Demo;

namespace SlateAi.Api.Features.Admin;

/// <param name="DemoEnabled">The demo is on on this server (<c>Demo:Enabled</c>); without it there is nothing to switch.</param>
/// <param name="SchoolDate">The demo teacher can act as a past school day (T-134, D-120). Off until switched on.</param>
/// <param name="UpdatedAt">When it was last switched; null: never.</param>
public sealed record AdminDemo(bool DemoEnabled, bool SchoolDate, DateTimeOffset? UpdatedAt);

public sealed record SetSchoolDateSwitchRequest(Guid ClientRequestId, bool On);

/// <summary>
/// The admin demo page (T-134): the switch that lets the demo teacher act as a past school day, to make test data
/// (D-120). One per server, stored in the database, off until switched on. Open to anyone with the address (D-112).
/// </summary>
public static class AdminDemoEndpoints
{
    /// <summary>Mapped by <see cref="AdminEndpoints.MapAdminEndpoints"/>.</summary>
    public static IEndpointRouteBuilder MapAdminDemoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/demo", GetAsync)
            .WithTags("Admin")
            .WithSummary("Whether the demo teacher can act as a past school day (T-134, D-120)")
            .WithDescription("schoolDate is off until switched on. demoEnabled false: the demo is off on this server. " +
                "Admin page; no key (D-112).");

        app.MapPut("/api/admin/demo/school-date", SetAsync)
            .WithTags("Admin")
            .WithSummary("Switch acting as a past school day on or off for the demo class (T-134, D-120)")
            .WithDescription("Body { clientRequestId, on }. While on, the demo teacher's app shows the school-day bar and " +
                "the server applies X-School-Date for the demo class; off, it is ignored and the bar goes. Idempotent " +
                "per clientRequestId. 400 without it; 409 when the demo is off on this server. Admin page; no key (D-112).");
        return app;
    }

    private static async Task<Ok<AdminDemo>> GetAsync(
        HttpContext http, IOptions<DemoOptions> demo, ISchoolDateSwitchStore store, CancellationToken cancellationToken)
    {
        var setting = await store.GetAsync(cancellationToken);
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(new AdminDemo(demo.Value.Enabled, demo.Value.Enabled && setting.On, setting.UpdatedAt));
    }

    private static async Task<Results<Ok<AdminDemo>, ValidationProblem, Conflict>> SetAsync(
        SetSchoolDateSwitchRequest request,
        HttpContext http,
        IOptions<DemoOptions> demo,
        ISchoolDateSwitchStore store,
        SchoolDateSwitch schoolDate,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        if (!demo.Value.Enabled)
        {
            return TypedResults.Conflict();
        }

        var setting = await store.SetAsync(request.ClientRequestId, request.On, clock.GetUtcNow(), cancellationToken);
        schoolDate.Remember(setting.On);
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(new AdminDemo(true, setting.On, setting.UpdatedAt));
    }
}
