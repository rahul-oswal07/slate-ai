using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;

namespace SlateAi.Api.Features.Admin;

/// <summary>One lesson audio on the admin day view, with its lesson names (English, from the curriculum).</summary>
/// <param name="ClassLabel">"Shobha · Grade 3": the class's main teacher and grade.</param>
public sealed record AdminLessonAudio(
    Guid Id,
    string ClassLabel,
    string Language,
    IReadOnlyList<string> Lessons,
    string Status,
    string? Verdict,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    Guid? ClipId,
    double? Seconds,
    string? Transcript,
    IReadOnlyList<AdminFamily> Families);

/// <param name="SentCount">Evening messages that day that carried a lesson audio.</param>
public sealed record AdminLessonAudioDay(DateOnly Date, int SentCount, IReadOnlyList<AdminLessonAudio> Audios);

/// <summary>
/// The admin pages (D-83): every teacher's greeting (T-66) and the lesson audio of a day (T-67). Read-only and linked
/// from nowhere. Mapped only when <c>Admin:Key</c> is set; every request must carry that key as <c>X-Admin-Key</c>,
/// and a missing or wrong key gets the same 404 as a route that doesn't exist.
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

        app.MapGet("/api/admin/greetings", ListGreetingsAsync)
            .WithTags("Admin")
            .WithSummary("Every teacher's recorded greeting, newest first")
            .WithDescription($"Needs the {KeyHeader} header; 404 without it. Admin only.");

        app.MapGet("/api/admin/greetings/{clipId:guid}/audio", (Guid clipId, HttpContext http, IAdminStore store,
                IOptions<AdminOptions> options, CancellationToken cancellationToken) =>
                GetClipAsync(clipId, AudioKind.Greeting, http, store, options, cancellationToken))
            .WithTags("Admin")
            .WithSummary("One greeting's audio (Ogg Opus)")
            .WithDescription($"Needs the {KeyHeader} header. 404 without it, or for a clip that isn't a greeting. Admin only.");

        app.MapGet("/api/admin/lesson-audio", GetLessonAudioDayAsync)
            .WithTags("Admin")
            .WithSummary("Lesson audio of one day: how many families got it, and every class's clip, sent or not")
            .WithDescription(
                $"date=YYYY-MM-DD (400 if missing or invalid). Needs the {KeyHeader} header; 404 without it. sentCount " +
                "counts that day's evening messages that carried a lesson audio; each audio lists the families it went " +
                "to, or none with its status and verdict when it wasn't sent. Admin only.");

        app.MapGet("/api/admin/lesson-audio/{clipId:guid}/audio", (Guid clipId, HttpContext http, IAdminStore store,
                IOptions<AdminOptions> options, CancellationToken cancellationToken) =>
                GetClipAsync(clipId, AudioKind.Lesson, http, store, options, cancellationToken))
            .WithTags("Admin")
            .WithSummary("One lesson audio clip (Ogg Opus)")
            .WithDescription($"Needs the {KeyHeader} header. 404 without it, or for a clip that isn't lesson audio. Admin only.");
        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<AdminGreeting>>, NotFound>> ListGreetingsAsync(
        HttpContext http, IAdminStore store, IOptions<AdminOptions> options, CancellationToken cancellationToken)
    {
        if (!HasKey(http, options.Value))
        {
            return TypedResults.NotFound();
        }

        NoStore(http);
        return TypedResults.Ok(await store.ListGreetingsAsync(cancellationToken));
    }

    private static async Task<Results<Ok<AdminLessonAudioDay>, NotFound, ValidationProblem>> GetLessonAudioDayAsync(
        string? date,
        HttpContext http,
        IAdminStore store,
        CurriculumGraph curriculum,
        IOptions<AdminOptions> options,
        CancellationToken cancellationToken)
    {
        if (!HasKey(http, options.Value))
        {
            return TypedResults.NotFound();
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Use YYYY-MM-DD."] });
        }

        var rows = await store.GetLessonAudioDayAsync(day, cancellationToken);
        var sent = await store.CountLessonAudioSentAsync(day, cancellationToken);
        var audios = rows
            .Select(r => new AdminLessonAudio(
                r.Id,
                $"{r.TeacherName} · Grade {r.Grade}",
                r.Language,
                r.CompetencyIds.Select(id => curriculum.Get(id)?.Name("en") ?? id).ToList(),
                r.Status,
                r.Verdict,
                r.ApprovedBy,
                r.ApprovedAt,
                r.ClipId,
                r.Seconds,
                r.Transcript,
                r.Families))
            .ToList();
        NoStore(http);
        return TypedResults.Ok(new AdminLessonAudioDay(day, sent, audios));
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetClipAsync(
        Guid clipId, string kind, HttpContext http, IAdminStore store, IOptions<AdminOptions> options, CancellationToken cancellationToken)
    {
        if (!HasKey(http, options.Value) || await store.GetClipAsync(clipId, kind, cancellationToken) is not { } clip)
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
