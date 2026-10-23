using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Audio;

/// <summary>Serves audio segments to the teacher (T3). Parents get theirs through the link token with T-15.</summary>
public static class AudioEndpoints
{
    public static IServiceCollection AddAudio(this IServiceCollection services)
    {
        services.AddScoped<IAudioStore, AudioStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapAudioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audio/{id:guid}", GetAsync)
            .RequireAuthorization()
            .WithTags("Audio")
            .WithSummary("One audio segment (Ogg Opus) of a catch-up: disclosure or script")
            .WithDescription(
                "For the signed-in teacher: her class's catch-up audio, or a shared clip (the spoken AI disclosure). " +
                "Supports range requests so phones can seek. 404 for another class's audio.");
        return app;
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetAsync(
        Guid id, HttpContext http, IAudioStore store, CancellationToken cancellationToken)
    {
        if (await store.GetInfoAsync(id, cancellationToken) is not { } info
            || (info.ClassId is { } classId && classId != http.User.GetClassId())
            || await store.GetBytesAsync(id, cancellationToken) is not { } bytes)
        {
            return TypedResults.NotFound();
        }

        var contentType = await store.GetContentTypeAsync(id, cancellationToken) ?? "application/octet-stream";
        http.Response.Headers.CacheControl = "private, max-age=86400";
        return TypedResults.File(bytes, contentType, enableRangeProcessing: true);
    }
}
