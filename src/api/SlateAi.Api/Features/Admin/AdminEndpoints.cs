using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.LessonLinks;
using SlateAi.Api.Features.Curricula;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.TextbookImport;

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
/// The admin pages (D-83): every teacher's greeting (T-66), the lesson audio of a day (T-67) and the lesson links
/// (T-94), and the children's WhatsApp numbers (T-118). Linked from nowhere and open to anyone with the address, in
/// every environment: no key (D-112, which replaced the key of D-83 and D-102).
/// </summary>
public static class AdminEndpoints
{
    public static IServiceCollection AddAdmin(this IServiceCollection services) =>
        services.AddScoped<IAdminStore, AdminStore>();

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/greetings", ListGreetingsAsync)
            .WithTags("Admin")
            .WithSummary("Every teacher's recorded greeting, newest first")
            .WithDescription("Admin page; no key (D-112).");

        app.MapGet("/api/admin/greetings/{clipId:guid}/audio", (Guid clipId, HttpContext http, IAdminStore store,
                CancellationToken cancellationToken) =>
                GetClipAsync(clipId, AudioKind.Greeting, http, store, cancellationToken))
            .WithTags("Admin")
            .WithSummary("One greeting's audio (Ogg Opus)")
            .WithDescription("404 for a clip that isn't a greeting. Admin page; no key (D-112).");

        app.MapGet("/api/admin/lesson-audio", GetLessonAudioDayAsync)
            .WithTags("Admin")
            .WithSummary("Lesson audio of one day: how many families got it, and every class's clip, sent or not")
            .WithDescription(
                "date=YYYY-MM-DD (400 if missing or invalid). sentCount " +
                "counts that day's evening messages that carried a lesson audio; each audio lists the families it went " +
                "to, or none with its status and verdict when it wasn't sent. Admin page; no key (D-112).");

        app.MapGet("/api/admin/lesson-audio/{clipId:guid}/audio", (Guid clipId, HttpContext http, IAdminStore store,
                CancellationToken cancellationToken) =>
                GetClipAsync(clipId, AudioKind.Lesson, http, store, cancellationToken))
            .WithTags("Admin")
            .WithSummary("One lesson audio clip (Ogg Opus)")
            .WithDescription("404 for a clip that isn't lesson audio. Admin page; no key (D-112).");

        app.MapLessonLinkEndpoints();
        app.MapLessonFeedbackEndpoints();
        app.MapLessonContentEndpoints();
        app.MapTextbookImportEndpoints();
        app.MapCurriculumAdminEndpoints();
        app.MapAdminChildEndpoints();
        app.MapAdminTeacherEndpoints();
        app.MapAdminDemoEndpoints();
        return app;
    }

    private static async Task<Ok<IReadOnlyList<AdminGreeting>>> ListGreetingsAsync(
        HttpContext http, IAdminStore store, CancellationToken cancellationToken)
    {
        NoStore(http);
        return TypedResults.Ok(await store.ListGreetingsAsync(cancellationToken));
    }

    private static async Task<Results<Ok<AdminLessonAudioDay>, ValidationProblem>> GetLessonAudioDayAsync(
        string? date,
        HttpContext http,
        IAdminStore store,
        ICurriculumProvider curricula,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Use YYYY-MM-DD."] });
        }

        var rows = await store.GetLessonAudioDayAsync(day, cancellationToken);
        var sent = await store.CountLessonAudioSentAsync(day, cancellationToken);
        // Each class names its lessons from its own curriculum (T-145).
        var curriculumOf = new Dictionary<Guid, CurriculumGraph>();
        foreach (var classId in rows.Select(r => r.ClassId).Distinct())
        {
            curriculumOf[classId] = await curricula.ForClassAsync(classId, cancellationToken);
        }

        var audios = rows
            .Select(r => new AdminLessonAudio(
                r.Id,
                $"{r.TeacherName} · Grade {r.Grade}",
                r.Language,
                r.CompetencyIds.Select(id => curriculumOf[r.ClassId].Get(id)?.Name("en") ?? id).ToList(),
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
        Guid clipId, string kind, HttpContext http, IAdminStore store, CancellationToken cancellationToken)
    {
        if (await store.GetClipAsync(clipId, kind, cancellationToken) is not { } clip)
        {
            return TypedResults.NotFound();
        }

        NoStore(http);
        return TypedResults.File(clip.Bytes, clip.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Admin responses are never cached and never indexed.</summary>
    internal static void NoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers["X-Robots-Tag"] = "noindex";
    }
}
