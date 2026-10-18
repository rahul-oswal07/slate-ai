using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Health;

public sealed class BuildInfoOptions
{
    public const string Section = "Build";

    /// <summary>Image tag baked in at build time (deploy/Dockerfile APP_VERSION); "dev" when run from source.</summary>
    public string Version { get; set; } = "dev";
}

public static class HealthEndpoints
{
    /// <summary>
    /// GET /health: 200 when the database is reachable, 503 otherwise. Reports status and build version only;
    /// the deploy smoke test uses the version to confirm the new container is the one answering.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteStatusAsync });
        return app;
    }

    private static Task WriteStatusAsync(HttpContext context, HealthReport report)
    {
        var version = context.RequestServices.GetRequiredService<IOptions<BuildInfoOptions>>().Value.Version;
        return context.Response.WriteAsJsonAsync(
            new HealthResponse(report.Status.ToString(), version), context.RequestAborted);
    }
}

public sealed record HealthResponse(string Status, string Version);
