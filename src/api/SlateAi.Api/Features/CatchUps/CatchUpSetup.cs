using SlateAi.Api.Features.CatchUps.Compression;

namespace SlateAi.Api.Features.CatchUps;

/// <summary>
/// The catch-up pipeline (FR-06–FR-09): A2 compression now; A3 script, A4 verifier, audio and the
/// <c>catchups/generate</c> endpoint follow (T-09–T-11, T-14).
/// </summary>
public static class CatchUpSetup
{
    public static IServiceCollection AddCatchUps(this IServiceCollection services)
    {
        services.AddScoped<Compressor>();
        return services;
    }
}
