using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SlateAi.Api.Features.Health;

public static class HealthEndpoints
{
    /// <summary>GET /health: 200 when the database is reachable, 503 otherwise. Status only, no internals.</summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteStatusAsync });
        return app;
    }

    private static Task WriteStatusAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthResponse(report.Status.ToString()), context.RequestAborted);
}

public sealed record HealthResponse(string Status);
