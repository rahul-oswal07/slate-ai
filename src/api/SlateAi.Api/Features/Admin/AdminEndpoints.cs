using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Admin;

/// <summary>
/// The admin page that plays every teacher's greeting (T-66, D-83). Read-only and linked from nowhere. Mapped only
/// when <c>Admin:Key</c> is set; every request must carry that key as <c>X-Admin-Key</c>, and a missing or wrong key
/// gets the same 404 as a route that doesn't exist.
/// </summary>
public static class AdminEndpoints
{
    public const string KeyHeader = "X-Admin-Key";

    public static IServiceCollection AddAdmin(this IServiceCollection services)
    {
        services.AddScoped<IAdminStore, AdminStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        if (!app.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>().Value.Enabled)
        {
            return app;
        }

        app.MapGet("/api/admin/greetings", ListAsync)
            .WithTags("Admin")
            .WithSummary("Every teacher's recorded greeting, newest first")
            .WithDescription($"Needs the {KeyHeader} header; 404 without it. Admin only.");

        app.MapGet("/api/admin/greetings/{clipId:guid}/audio", GetAudioAsync)
            .WithTags("Admin")
            .WithSummary("One greeting's audio (Ogg Opus)")
            .WithDescription($"Needs the {KeyHeader} header. 404 without it, or for a clip that isn't a greeting. Admin only.");
        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<AdminGreeting>>, NotFound>> ListAsync(
        HttpContext http, IAdminStore store, IOptions<AdminOptions> options, CancellationToken cancellationToken)
    {
        if (!HasKey(http, options.Value))
        {
            return TypedResults.NotFound();
        }

        NoStore(http);
        return TypedResults.Ok(await store.ListGreetingsAsync(cancellationToken));
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetAudioAsync(
        Guid clipId, HttpContext http, IAdminStore store, IOptions<AdminOptions> options, CancellationToken cancellationToken)
    {
        if (!HasKey(http, options.Value) || await store.GetGreetingAsync(clipId, cancellationToken) is not { } clip)
        {
            return TypedResults.NotFound();
        }

        NoStore(http);
        return TypedResults.File(clip.Bytes, clip.ContentType, enableRangeProcessing: true);
    }

    private static bool HasKey(HttpContext http, AdminOptions options) =>
        options.Enabled
        && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(http.Request.Headers[KeyHeader].ToString()),
            Encoding.UTF8.GetBytes(options.Key!));

    private static void NoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers["X-Robots-Tag"] = "noindex";
    }
}
