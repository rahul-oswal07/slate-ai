using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Admin;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>An import as the admin page shows it (T-146).</summary>
/// <param name="Status">queued, running, ready or failed (a running import that stopped moving reads as failed).</param>
/// <param name="Stage">While running: outline, lessons, prerequisites or assemble.</param>
/// <param name="Lessons">Lessons in the draft, once ready.</param>
/// <param name="Flagged">Lessons in the draft with something to look at.</param>
public sealed record TextbookImportView(
    Guid Id,
    string Title,
    int Grade,
    string Subject,
    string Medium,
    string Book,
    int? Part,
    int PageCount,
    string Status,
    string? Stage,
    int StepsDone,
    int StepsTotal,
    string? Error,
    IReadOnlyList<string> Notes,
    Guid? CurriculumId,
    int? Lessons,
    int? Flagged,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RetryImportRequest(Guid ClientRequestId);

/// <summary>
/// Upload a textbook and follow its import on <c>/admin/curriculum</c> (T-146, D-125). Open to anyone with the address,
/// like the other admin pages (D-112). Reviewing and publishing the draft is T-147.
/// </summary>
public static partial class TextbookImportEndpoints
{
    public const long MaxPdfBytes = 100L * 1024 * 1024;
    public const int MaxPages = 600;

    private static readonly string[] Media = ["kn", "en", "te", "ta", "hi"];
    private static readonly string[] Books = ["textbook", "workbook", TextbookPage.Nalikali];

    public static IServiceCollection AddTextbookImports(this IServiceCollection services)
    {
        services.AddScoped<ITextbookImportStore, TextbookImportStore>();
        services.AddScoped<TextbookImporter>();
        services.AddSingleton<TextbookImportQueue>();
        services.AddHostedService<TextbookImportWorker>();
        return services;
    }

    /// <summary>Mapped by <see cref="AdminEndpoints.MapAdminEndpoints"/>.</summary>
    public static IEndpointRouteBuilder MapTextbookImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/curriculum-imports", UploadAsync)
            .DisableAntiforgery() // JSON-only app; same as the other uploads
            .WithMetadata(new RequestSizeLimitAttribute(MaxPdfBytes + 1024 * 1024))
            .WithFormOptions(multipartBodyLengthLimit: MaxPdfBytes + 1024 * 1024)
            .WithTags("Admin")
            .WithSummary("Upload a textbook to turn into a draft curriculum (T-146)")
            .WithDescription(
                "multipart: pdf (≤ 100 MB, ≤ 600 pages), clientRequestId, title, grade (1–5), subject (e.g. maths), " +
                "medium (kn, en, te, ta, hi), book (textbook, workbook, nalikali), part (optional). Returns 202 with the " +
                "import, queued: Claude reads the book in the background (A10) and code checks everything into a draft. " +
                "The same clientRequestId returns the same import. 400 for a field or a PDF that can't be read, 413 too " +
                "big, 415 not a PDF. The book's pages are kept only until the draft is written. Admin page; no key (D-112).");

        app.MapGet("/api/admin/curriculum-imports", ListAsync)
            .WithTags("Admin")
            .WithSummary("Every textbook import, newest first, with its progress (T-146)")
            .WithDescription("A running import that hasn't moved for 10 minutes reads as failed (error:stale). Admin page; no key (D-112).");

        app.MapGet("/api/admin/curriculum-imports/{id:guid}", GetAsync)
            .WithTags("Admin")
            .WithSummary("One textbook import (T-146)")
            .WithDescription("Its progress, notes about the book, and once ready its draft's lesson and flag counts. Admin page; no key (D-112).");

        app.MapPost("/api/admin/curriculum-imports/{id:guid}/retry", RetryAsync)
            .WithTags("Admin")
            .WithSummary("Try a failed or stopped import again (T-146)")
            .WithDescription(
                "Carries on where it stopped: finished steps aren't read again. Nothing happens to an import that is " +
                "queued, running or ready. 404 unknown. Admin page; no key (D-112).");

        app.MapDelete("/api/admin/curriculum-imports/{id:guid}", DiscardAsync)
            .WithTags("Admin")
            .WithSummary("Discard an import, its pages and its draft (T-146)")
            .WithDescription("204, also when it is already gone. A published curriculum is never deleted. Admin page; no key (D-112).");
        return app;
    }

    private static async Task<Results<Accepted<TextbookImportView>, ValidationProblem, StatusCodeHttpResult>> UploadAsync(
        IFormFile? pdf,
        [FromForm] Guid clientRequestId,
        [FromForm] string? title,
        [FromForm] int? grade,
        [FromForm] string? subject,
        [FromForm] string? medium,
        [FromForm] string? book,
        [FromForm] int? part,
        HttpContext http,
        ITextbookImportStore store,
        TextbookImportQueue queue,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        AdminEndpoints.NoStore(http);
        var (info, errors) = Check(clientRequestId, title, grade, subject, medium, book, part);
        if (pdf is null || pdf.Length == 0)
        {
            errors["pdf"] = ["Choose the textbook's PDF."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        // A replay (the page sent it twice) gets the same import without reading the book again.
        if (await store.FindByRequestAsync(clientRequestId, cancellationToken) is { } replayed)
        {
            return TypedResults.Accepted((string?)null, await ViewAsync(replayed, store, clock, cancellationToken));
        }

        if (pdf!.Length > MaxPdfBytes)
        {
            return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var bytes = await BytesAsync(pdf, cancellationToken);
        if ((pdf.ContentType ?? "").Split(';')[0].Trim().ToLowerInvariant() != "application/pdf" || !bytes.AsSpan().StartsWith("%PDF-"u8))
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        IReadOnlyList<byte[]> pages;
        try
        {
            pages = PdfPages.Split(bytes, MaxPages);
        }
        catch (PdfUnreadableException e)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["pdf"] = [e.Message] });
        }

        var (import, created) = await store.CreateAsync(clientRequestId, info!, pages, clock.GetUtcNow(), cancellationToken);
        if (created && !queue.TryEnqueue(new TextbookImportJob(import.Id)))
        {
            await store.FailAsync(import.Id, "error:queue-full", clock.GetUtcNow(), cancellationToken);
        }

        return TypedResults.Accepted((string?)null, await ViewAsync(import, store, clock, cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<TextbookImportView>>> ListAsync(
        HttpContext http, ITextbookImportStore store, TimeProvider clock, CancellationToken cancellationToken)
    {
        var views = new List<TextbookImportView>();
        foreach (var import in await store.ListAsync(cancellationToken))
        {
            views.Add(await ViewAsync(import, store, clock, cancellationToken));
        }

        AdminEndpoints.NoStore(http);
        return TypedResults.Ok<IReadOnlyList<TextbookImportView>>(views);
    }

    private static async Task<Results<Ok<TextbookImportView>, NotFound>> GetAsync(
        Guid id, HttpContext http, ITextbookImportStore store, TimeProvider clock, CancellationToken cancellationToken)
    {
        AdminEndpoints.NoStore(http);
        return await store.GetAsync(id, cancellationToken) is { } import
            ? TypedResults.Ok(await ViewAsync(import, store, clock, cancellationToken))
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<TextbookImportView>, NotFound, ValidationProblem>> RetryAsync(
        Guid id,
        RetryImportRequest request,
        HttpContext http,
        ITextbookImportStore store,
        TextbookImportQueue queue,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        var now = clock.GetUtcNow();
        switch (await store.RetryAsync(id, now - TextbookImporter.StaleAfter, now, cancellationToken))
        {
            case ImportRetry.NotFound:
                return TypedResults.NotFound();
            case ImportRetry.Queued when !queue.TryEnqueue(new TextbookImportJob(id)):
                await store.FailAsync(id, "error:queue-full", now, cancellationToken);
                break;
        }

        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(await ViewAsync((await store.GetAsync(id, cancellationToken))!, store, clock, cancellationToken));
    }

    private static async Task<NoContent> DiscardAsync(
        Guid id, HttpContext http, ITextbookImportStore store, CancellationToken cancellationToken)
    {
        await store.DiscardAsync(id, cancellationToken);
        AdminEndpoints.NoStore(http);
        return TypedResults.NoContent();
    }

    /// <summary>The upload's fields, trimmed, or the errors by field.</summary>
    internal static (ImportBook? Book, Dictionary<string, string[]> Errors) Check(
        Guid clientRequestId, string? title, int? grade, string? subject, string? medium, string? book, int? part)
    {
        var errors = new Dictionary<string, string[]>();
        if (clientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        var name = title?.Trim() ?? "";
        if (name.Length is 0 or > 200)
        {
            errors["title"] = ["Give the book a name of up to 200 characters."];
        }

        if (grade is not (>= 1 and <= 5))
        {
            errors["grade"] = ["Grade 1 to 5."];
        }

        var topic = subject?.Trim().ToLowerInvariant() ?? "";
        if (!SubjectFormat().IsMatch(topic))
        {
            errors["subject"] = ["One word in small letters, e.g. maths."];
        }

        if (!Media.Contains(medium))
        {
            errors["medium"] = ["One of kn, en, te, ta, hi."];
        }

        if (!Books.Contains(book))
        {
            errors["book"] = ["One of textbook, workbook, nalikali."];
        }

        if (part is not (null or (>= 1 and <= 9)))
        {
            errors["part"] = ["1 to 9, or leave it out."];
        }

        return errors.Count > 0 ? (null, errors) : (new ImportBook(name, grade!.Value, topic, medium!, book!, part), errors);
    }

    internal static TextbookImportView View(CurriculumImport import, (int Lessons, int Flagged)? draft, DateTimeOffset now)
    {
        // The queue lives in memory: an import that stopped moving (a restart) can only be tried again.
        var stale = import.Status is CurriculumImportStatuses.Running or CurriculumImportStatuses.Queued
                    && now - import.UpdatedAt > TextbookImporter.StaleAfter;
        return new TextbookImportView(
            import.Id, import.Title, import.Grade, import.Subject, import.Medium, import.Book, import.Part, import.PageCount,
            stale ? CurriculumImportStatuses.Failed : import.Status,
            stale ? null : import.Stage,
            import.StepsDone,
            import.StepsTotal,
            stale ? "error:stale" : import.Error,
            import.Notes,
            import.CurriculumId,
            draft?.Lessons,
            draft?.Flagged,
            import.CreatedAt,
            import.UpdatedAt);
    }

    private static async Task<TextbookImportView> ViewAsync(
        CurriculumImport import, ITextbookImportStore store, TimeProvider clock, CancellationToken cancellationToken) =>
        View(import, import.CurriculumId is null ? null : await store.CountDraftAsync(import.Id, cancellationToken), clock.GetUtcNow());

    private static async Task<byte[]> BytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    [GeneratedRegex("^[a-z]{2,30}$")]
    private static partial Regex SubjectFormat();
}
