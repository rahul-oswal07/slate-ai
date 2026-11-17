using System.Text.Json;
using System.Threading.Channels;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>
/// Runs one import (T-146, D-125): outline (A10, a few pages at a time) → lessons (A10, a chapter part at a time) →
/// prerequisites (A10, text only) → the draft, written by code. Each finished step is saved, so "Try again" carries on
/// where it stopped. Claude sees only the book's pages and IDs: no names (guardrail 1); everything it returns is checked
/// (guardrail 2) and no answer is stored (guardrail 4).
/// </summary>
public sealed class TextbookImporter(
    ITextbookImportStore store,
    ILlmClient llm,
    IAiCallStore aiCalls,
    PromptLibrary prompts,
    TimeProvider clock,
    ILogger<TextbookImporter> logger)
{
    /// <summary>Pages per outline call.</summary>
    public const int MaxPagesPerWindow = 10;

    /// <summary>PDF bytes per call. Base64 makes it about 27 MB, under Claude's 32 MB request limit.</summary>
    public const int MaxRequestBytes = 20 * 1024 * 1024;

    /// <summary>A running import that hasn't moved for this long stopped (the app restarted): it reads as failed.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task RunAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await store.TryStartAsync(id, clock.GetUtcNow(), cancellationToken) is not { } import)
        {
            return; // not queued: another run has it, or it's done
        }

        try
        {
            await RunStagesAsync(import, cancellationToken);
        }
        catch (StepFailedException e)
        {
            logger.LogWarning("Textbook import {Id} stopped: {Error}", id, e.Message);
            await store.FailAsync(id, e.Message, clock.GetUtcNow(), CancellationToken.None);
        }
    }

    /// <summary>
    /// Consecutive runs of pages (in the order given), each at most <paramref name="maxPages"/> pages and
    /// <paramref name="maxBytes"/> bytes; a page bigger than that on its own goes alone. Pure.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<int>> Plan(
        IReadOnlyList<int> pages, IReadOnlyList<int> sizes, int maxPages, int maxBytes)
    {
        var runs = new List<IReadOnlyList<int>>();
        var current = new List<int>();
        var bytes = 0;
        foreach (var page in pages)
        {
            var size = sizes[page - 1];
            if (current.Count > 0 && (current.Count == maxPages || bytes + size > maxBytes))
            {
                runs.Add(current);
                current = [];
                bytes = 0;
            }

            current.Add(page);
            bytes += size;
        }

        if (current.Count > 0)
        {
            runs.Add(current);
        }

        return runs;
    }

    private async Task RunStagesAsync(CurriculumImport import, CancellationToken cancellationToken)
    {
        var book = new ImportBook(import.Title, import.Grade, import.Subject, import.Medium, import.Book, import.Part);
        var steps = await store.GetStepsAsync(import.Id, cancellationToken);
        var sizes = await store.GetPageSizesAsync(import.Id, cancellationToken);
        if (sizes.Count != import.PageCount)
        {
            throw new StepFailedException("error:pages-missing");
        }

        // 1. Outline.
        var windows = Plan(Enumerable.Range(1, import.PageCount).ToList(), sizes, MaxPagesPerWindow, MaxRequestBytes);
        var done = 0;
        var total = windows.Count + 1;
        await store.SetProgressAsync(import.Id, CurriculumImportStages.Outline, done, total, clock.GetUtcNow(), cancellationToken);
        var pages = new List<OutlinePage>();
        foreach (var window in windows)
        {
            var (first, last) = (window[0], window[^1]);
            var key = $"outline:{first}-{last}";
            pages.AddRange(await StepAsync(import.Id, steps, key, ++done, async () =>
            {
                var response = await CallAsync(
                    TextbookOutline.PromptId, TextbookOutline.UserContent(book, first, window.Count), TextbookOutline.Schema(),
                    window, 8192, LlmEffort.Medium, $"reading pages {first}–{last}", import.Id, cancellationToken);
                return await CheckedAsync(response, TextbookOutline.Validate(response.Json, first, window.Count), $"reading pages {first}–{last}", cancellationToken);
            }, cancellationToken));
        }

        var outline = TextbookOutline.Assemble(pages) ?? throw new StepFailedException("fallback:no-chapters");

        // 2. Lessons, chapter by chapter.
        var parts = outline.Chapters
            .Select(c => (Chapter: c, Parts: Plan(c.Pages, sizes, TextbookLessons.MaxPagesPerPart, MaxRequestBytes)))
            .ToList();
        total = windows.Count + parts.Sum(c => c.Parts.Count) + 1;
        await store.SetProgressAsync(import.Id, CurriculumImportStages.Lessons, done, total, clock.GetUtcNow(), cancellationToken);
        var chapters = new List<(OutlineChapter, IReadOnlyList<ExtractedLesson>)>();
        foreach (var (chapter, chapterParts) in parts)
        {
            var found = new List<IReadOnlyList<ExtractedLesson>>();
            for (var i = 0; i < chapterParts.Count; i++)
            {
                var part = chapterParts[i];
                var number = i + 1;
                var earlier = TextbookLessons.Merge(found);
                var what = $"reading chapter {chapter.Number}" + (chapterParts.Count > 1 ? $", part {number} of {chapterParts.Count}" : "");
                found.Add(await StepAsync(import.Id, steps, $"lessons:{chapter.Number}:{number}", ++done, async () =>
                {
                    var response = await CallAsync(
                        TextbookLessons.PromptId,
                        TextbookLessons.UserContent(book, chapter, number, chapterParts.Count, part, outline.PrintedPages, earlier),
                        TextbookLessons.Schema(), part, 16000, LlmEffort.High, what, import.Id, cancellationToken);
                    return await CheckedAsync(response, TextbookLessons.Validate(response.Json, part, outline.PrintedPages), what, cancellationToken);
                }, cancellationToken));
            }

            chapters.Add((chapter, TextbookLessons.Merge(found)));
        }

        var lessons = DraftAssembler.Lessons(book, outline, chapters);
        if (lessons.Count == 0)
        {
            throw new StepFailedException("fallback:no-lessons");
        }

        // 3. Prerequisites: without them the draft is still useful, so a failure here is a note, not a stop.
        await store.SetProgressAsync(import.Id, CurriculumImportStages.Prerequisites, done, total, clock.GetUtcNow(), cancellationToken);
        var notes = outline.Flags.ToList();
        var prerequisites = await PrerequisitesAsync(import.Id, book, lessons, steps, ++done, cancellationToken);
        if (prerequisites is null)
        {
            notes.Add("Prerequisites couldn't be worked out; every lesson has none until they are added.");
        }

        // 4. The draft.
        await store.SetProgressAsync(import.Id, CurriculumImportStages.Assemble, done, total, clock.GetUtcNow(), cancellationToken);
        DraftCurriculum draft;
        try
        {
            draft = DraftAssembler.Finish(outline, lessons, prerequisites);
        }
        catch (CurriculumException e)
        {
            logger.LogError("Textbook import {Id}: the draft broke a curriculum rule: {Errors}", import.Id, string.Join("; ", e.Errors));
            throw new StepFailedException("error:invalid-draft");
        }

        var curriculumId = await store.SaveDraftAsync(import.Id, draft, notes, clock.GetUtcNow(), cancellationToken);
        logger.LogInformation(
            "Textbook import {Id}: draft {CurriculumId} with {Chapters} chapters and {Lessons} lessons, {Flagged} flagged",
            import.Id, curriculumId, draft.Chapters.Count, draft.Lessons.Count, draft.Lessons.Count(l => l.Flags.Count > 0));
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>?> PrerequisitesAsync(
        Guid id, ImportBook book, IReadOnlyList<DraftLesson> lessons, IReadOnlyDictionary<string, string> steps, int done,
        CancellationToken cancellationToken)
    {
        const string key = "prerequisites";
        if (steps.TryGetValue(key, out var saved))
        {
            return JsonSerializer.Deserialize<Dictionary<string, IReadOnlyList<string>>>(saved, Json);
        }

        var ids = lessons.Select(l => l.Competency.Id).ToList();
        LlmResponse response;
        try
        {
            response = await llm.CompleteJsonAsync(
                new LlmRequest(
                    prompts.Get(TextbookPrerequisites.PromptId), TextbookPrerequisites.UserContent(book, lessons),
                    TextbookPrerequisites.Schema(ids), MaxTokens: 8192, Effort: LlmEffort.Medium),
                cancellationToken);
        }
        catch (LlmException)
        {
            return null; // the AiCall row records why
        }

        var result = TextbookPrerequisites.Validate(response.Json, lessons.ToDictionary(l => l.Competency.Id, l => l.Competency.Sequence));
        await VerdictAsync(response, result is null ? "fallback:invalid-output" : "valid", cancellationToken);
        if (result is not null)
        {
            await store.SaveStepAsync(id, key, JsonSerializer.Serialize(result, Json), done, clock.GetUtcNow(), cancellationToken);
        }

        return result;
    }

    /// <summary>The step's saved result, or runs it and saves what it returns.</summary>
    private async Task<T> StepAsync<T>(
        Guid id, IReadOnlyDictionary<string, string> steps, string key, int done, Func<Task<T>> run, CancellationToken cancellationToken)
    {
        if (steps.TryGetValue(key, out var saved))
        {
            return JsonSerializer.Deserialize<T>(saved, Json)!;
        }

        var result = await run();
        await store.SaveStepAsync(id, key, JsonSerializer.Serialize(result, Json), done, clock.GetUtcNow(), cancellationToken);
        return result;
    }

    private async Task<LlmResponse> CallAsync(
        string promptId, string userContent, JsonElement schema, IReadOnlyList<int> pages, int maxTokens, LlmEffort effort,
        string what, Guid id, CancellationToken cancellationToken)
    {
        var pdf = PdfPages.Join(await store.GetPagesAsync(id, pages, cancellationToken));
        try
        {
            return await llm.CompleteJsonAsync(
                new LlmRequest(prompts.Get(promptId), userContent, schema, maxTokens, effort, Documents: [new LlmDocument(pdf)]),
                cancellationToken);
        }
        catch (LlmException e)
        {
            throw new StepFailedException($"error:{e.Failure} {what}");
        }
    }

    private async Task<T> CheckedAsync<T>(LlmResponse response, T? result, string what, CancellationToken cancellationToken)
        where T : class
    {
        await VerdictAsync(response, result is null ? "fallback:invalid-output" : "valid", cancellationToken);
        return result ?? throw new StepFailedException($"fallback:invalid-output {what}");
    }

    private async Task VerdictAsync(LlmResponse response, string verdict, CancellationToken cancellationToken)
    {
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, verdict, cancellationToken);
        }
    }

    /// <summary>A step can't go on: the import fails with this message (short; never book text) and can be tried again.</summary>
    private sealed class StepFailedException(string message) : Exception(message);
}

public sealed record TextbookImportJob(Guid ImportId);

public sealed class TextbookImportQueue
{
    private readonly Channel<TextbookImportJob> _jobs = Channel.CreateBounded<TextbookImportJob>(
        new BoundedChannelOptions(20) { FullMode = BoundedChannelFullMode.DropWrite });

    public bool TryEnqueue(TextbookImportJob job) => _jobs.Writer.TryWrite(job);

    public IAsyncEnumerable<TextbookImportJob> ReadAllAsync(CancellationToken cancellationToken) => _jobs.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>Runs queued imports one at a time, each in its own scope, outside the request.</summary>
public sealed class TextbookImportWorker(
    TextbookImportQueue queue, IServiceScopeFactory scopes, TimeProvider clock, ILogger<TextbookImportWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopes.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<TextbookImporter>().RunAsync(job.ImportId, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Never let one book stop the worker; the import shows it failed and can be tried again.
                logger.LogError(e, "Textbook import {Id} failed", job.ImportId);
                await scope.ServiceProvider.GetRequiredService<ITextbookImportStore>().FailAsync(
                    job.ImportId, "error:exception", clock.GetUtcNow(), stoppingToken);
            }
        }
    }
}
