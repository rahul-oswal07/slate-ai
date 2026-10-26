using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.Greeting;

/// <summary>
/// The teacher's own greeting (T0 step 1, FR-09, D-44): about ten seconds recorded once with her voice consent, played
/// first in every catch-up and evening message. The phone sends 16 kHz mono WAV (the server has no ffmpeg) and it is
/// stored as Ogg Opus like every other clip. The audio is only stored: it is never sent to speech-to-text or an LLM,
/// because a greeting may say a child's name.
/// </summary>
public static class GreetingEndpoints
{
    public const long MaxBytes = 1024 * 1024;
    public const double MinSeconds = 1;
    public const double MaxSeconds = 12;

    private static readonly string[] WavTypes = ["audio/wav", "audio/x-wav", "audio/wave"];

    public static IServiceCollection AddGreeting(this IServiceCollection services)
    {
        services.AddScoped<IGreetingStore, GreetingStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapGreetingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/me/greeting", SaveAsync)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Greeting")
            .WithSummary("Save the signed-in teacher's greeting and her voice consent")
            .WithDescription(
                $"Multipart: file (16 kHz mono 16-bit WAV, {MinSeconds}-{MaxSeconds} s, up to 1 MB), clientRequestId and " +
                "consent=true. Replaces her previous greeting, sets Teacher.ConsentAt the first time and returns /api/me. " +
                "400 without consent or outside the length; 413 too large; 415 not a WAV. Idempotent by clientRequestId.");
        return app;
    }

    private static async Task<Results<Ok<MeResponse>, ValidationProblem, UnauthorizedHttpResult, StatusCodeHttpResult>> SaveAsync(
        IFormFile? file,
        [Microsoft.AspNetCore.Mvc.FromForm] Guid clientRequestId,
        [Microsoft.AspNetCore.Mvc.FromForm] bool consent,
        HttpContext http,
        IGreetingStore store,
        IAuthStore auth,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (http.User.GetTeacherId() is not { } teacherId)
        {
            return TypedResults.Unauthorized();
        }

        var errors = new Dictionary<string, string[]>();
        if (clientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        if (!consent)
        {
            errors["consent"] = ["Tick the box to agree."];
        }

        if (file is null || file.Length == 0)
        {
            errors["file"] = ["Send the recording."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (file!.Length > MaxBytes)
        {
            return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var contentType = (file.ContentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        if (!WavTypes.Contains(contentType))
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        byte[] ogg;
        double seconds;
        try
        {
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            (ogg, seconds) = OpusEncoding.FromWav(buffer.ToArray());
        }
        catch (InvalidDataException)
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        if (seconds is < MinSeconds or > MaxSeconds)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = [$"The greeting is {MinSeconds} to {MaxSeconds} seconds."],
            });
        }

        await store.SaveAsync(teacherId, clientRequestId, ogg, seconds, clock.GetUtcNow(), cancellationToken);
        var me = await auth.GetMeAsync(teacherId, cancellationToken);
        return me is null ? TypedResults.Unauthorized() : TypedResults.Ok(me);
    }
}
