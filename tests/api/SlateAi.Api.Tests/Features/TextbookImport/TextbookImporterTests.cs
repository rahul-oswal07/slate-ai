using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.TextbookImport;
using SlateAi.Api.Tests.Infrastructure;
using static SlateAi.Api.Tests.Features.TextbookImport.TextbookStepsTests;

namespace SlateAi.Api.Tests.Features.TextbookImport;

/// <summary>T-146: one import from upload to draft, with a scripted Claude and the in-memory store.</summary>
public sealed class TextbookImporterTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTextbookImportStore _store = new();
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly ScriptedLlm _llm = new();

    [Fact]
    public async Task A_book_becomes_a_draft_with_chapters_lessons_and_prerequisites_and_its_pages_are_deleted()
    {
        var import = _store.Add(Book, Pages(12), Now);

        await Importer().RunAsync(import.Id, Ct);

        var saved = _store.Imports[import.Id];
        Assert.Equal((CurriculumImportStatuses.Ready, (string?)null), (saved.Status, saved.Error));
        var draft = _store.Drafts[import.Id].Draft;
        Assert.Equal(["Numbers", "Addition"], draft.Chapters.Select(c => c.NameEn));
        Assert.Equal(["M3.NUM.TENS", "M3.ADD.CARRY"], draft.Lessons.Select(l => l.Competency.Id));
        Assert.Equal(["M3.NUM.TENS"], draft.Lessons[1].Competency.Prerequisites);
        Assert.Equal(6, draft.Lessons[1].Competency.TextbookPage!.Page); // PDF page 7 is printed 6
        Assert.Empty(_store.Pages[import.Id]);
        // Two outline windows (10 + 2 pages), one lessons call per chapter, one for prerequisites.
        Assert.Equal(
            [TextbookOutline.PromptId, TextbookOutline.PromptId, TextbookLessons.PromptId, TextbookLessons.PromptId, TextbookPrerequisites.PromptId],
            _llm.Requests.Select(r => r.Prompt.Id));
        Assert.All(_llm.Requests.Take(4), r => Assert.NotEmpty(Assert.Single(r.Documents!).Pdf));
        Assert.Null(_llm.Requests[^1].Documents); // prerequisites are text only
        Assert.Equal((5, 5), (saved.StepsDone, saved.StepsTotal));
    }

    [Fact]
    public async Task A_failed_step_stops_the_import_and_try_again_carries_on_without_reading_finished_pages_again()
    {
        var import = _store.Add(Book, Pages(12), Now);
        _llm.FailOn = TextbookLessons.PromptId;

        await Importer().RunAsync(import.Id, Ct);

        Assert.Equal(CurriculumImportStatuses.Failed, _store.Imports[import.Id].Status);
        Assert.Equal("error:Unavailable reading chapter 1", _store.Imports[import.Id].Error);
        Assert.Equal(["outline:1-10", "outline:11-12"], _store.Steps[import.Id].Keys.Order());

        _llm.FailOn = null;
        _llm.Requests.Clear();
        Assert.Equal(ImportRetry.Queued, await _store.RetryAsync(import.Id, Now, Now, Ct));
        await Importer().RunAsync(import.Id, Ct);

        Assert.Equal(CurriculumImportStatuses.Ready, _store.Imports[import.Id].Status);
        Assert.DoesNotContain(_llm.Requests, r => r.Prompt.Id == TextbookOutline.PromptId);
    }

    [Fact]
    public async Task Prerequisites_that_fail_leave_a_draft_without_them_and_a_note()
    {
        var import = _store.Add(Book, Pages(12), Now);
        _llm.FailOn = TextbookPrerequisites.PromptId;

        await Importer().RunAsync(import.Id, Ct);

        var saved = _store.Imports[import.Id];
        Assert.Equal(CurriculumImportStatuses.Ready, saved.Status);
        Assert.Contains(saved.Notes, n => n.StartsWith("Prerequisites couldn't", StringComparison.Ordinal));
        Assert.All(_store.Drafts[import.Id].Draft.Lessons, l => Assert.Empty(l.Competency.Prerequisites));
    }

    [Fact]
    public async Task Output_that_fails_the_checks_stops_the_import_with_its_verdict_recorded()
    {
        var import = _store.Add(Book, Pages(3), Now);
        _llm.Answer = _ => "not json";

        await Importer().RunAsync(import.Id, Ct);

        Assert.Equal("fallback:invalid-output reading pages 1–3", _store.Imports[import.Id].Error);
        Assert.Equal("fallback:invalid-output", Assert.Single(_aiCalls.Calls).Verdict);
    }

    [Fact]
    public async Task A_book_without_chapters_fails_and_an_import_that_is_not_queued_is_left_alone()
    {
        var noChapters = _store.Add(Book, Pages(3), Now);
        _llm.Answer = request => Json(new { pages = Enumerable.Range(1, 3).Select(p => OutlinePage(p, p, "lesson")) });
        await Importer().RunAsync(noChapters.Id, Ct);
        Assert.Equal("fallback:no-chapters", _store.Imports[noChapters.Id].Error);

        var running = _store.Add(Book, Pages(3), Now);
        _store.Imports[running.Id].Status = CurriculumImportStatuses.Running;
        _llm.Requests.Clear();
        await Importer().RunAsync(running.Id, Ct);
        Assert.Empty(_llm.Requests);
    }

    [Fact]
    public void Pages_are_planned_in_runs_of_at_most_n_pages_and_bytes_and_a_big_page_goes_alone()
    {
        int[] sizes = [5, 5, 5, 30, 5, 5];

        var runs = TextbookImporter.Plan([1, 2, 3, 4, 5, 6], sizes, maxPages: 2, maxBytes: 12);

        Assert.Equal(["1,2", "3", "4", "5,6"], runs.Select(r => string.Join(",", r)));
        Assert.Equal(["2,4", "6"], TextbookImporter.Plan([2, 4, 6], sizes, maxPages: 3, maxBytes: 39).Select(r => string.Join(",", r)));
    }

    private TextbookImporter Importer() => new(
        _store,
        _llm.Logged(_aiCalls),
        _aiCalls,
        PromptLibrary.Load(
            new[] { TextbookOutline.PromptId, TextbookLessons.PromptId, TextbookPrerequisites.PromptId }
                .Select(id => ($"{id}.md", $"---\nid: {id}\nversion: 1\nmodel_key: Import\n---\nPrompt.")),
            ["Import"]),
        new FixedClock(Now),
        NullLogger<TextbookImporter>.Instance);

    private static List<byte[]> Pages(int count)
    {
        var book = PdfPagesTests.Book(count);
        return [.. PdfPages.Split(book, 600)];
    }

    /// <summary>
    /// Claude for a 12-page book: chapter 1 (Numbers) starts on PDF page 3, chapter 2 (Addition) on page 7; page n is
    /// printed n − 1. Each chapter has one lesson; ADD.CARRY needs NUM.TENS.
    /// </summary>
    private sealed class ScriptedLlm : ILlmClient
    {
        public List<LlmRequest> Requests { get; } = [];

        public string? FailOn { get; set; }

        public Func<LlmRequest, string>? Answer { get; set; }

        /// <summary>Records a verdict-able call like <c>LoggedLlmClient</c>.</summary>
        public ILlmClient Logged(FakeAiCallStore calls) => new Logging(this, calls);

        public Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Prompt.Id == FailOn)
            {
                throw new LlmException(LlmFailure.Unavailable, "down");
            }

            return Task.FromResult(new LlmResponse(Answer?.Invoke(request) ?? Respond(request), "claude-opus-5-5", new LlmUsage(1, 1, 0), Guid.Empty));
        }

        private static string Respond(LlmRequest request)
        {
            using var content = JsonDocument.Parse(request.UserContent);
            return request.Prompt.Id switch
            {
                TextbookOutline.PromptId => Outline(content.RootElement.GetProperty("firstPdfPage").GetInt32(), content.RootElement.GetProperty("pages").GetInt32()),
                TextbookLessons.PromptId => content.RootElement.GetProperty("chapter").GetProperty("number").GetInt32() == 1
                    ? Json(new { lessons = new[] { Lesson(key: "NUM.TENS", nameEn: "Tens and ones") } })
                    : Json(new { lessons = new[] { Lesson(key: "ADD.CARRY") } }),
                _ => Json(new { lessons = new object[] { new { id = "M3.ADD.CARRY", prerequisites = new[] { "M3.NUM.TENS" } } } }),
            };
        }

        private static string Outline(int first, int count) => Json(new
        {
            pages = Enumerable.Range(1, count).Select(i =>
            {
                var page = first + i - 1;
                return page switch
                {
                    1 => OutlinePage(i, 0, "front"),
                    2 => OutlinePage(i, 1, "contents", starts: true, number: 1, kn: "ಸಂಖ್ಯೆಗಳು", en: "Numbers"),
                    3 => OutlinePage(i, 2, "lesson", starts: true, number: 1, kn: "ಸಂಖ್ಯೆಗಳು", en: "Numbers"),
                    7 => OutlinePage(i, 6, "lesson", starts: true, number: 2, kn: "ಕೂಡುವುದು", en: "Addition"),
                    _ => OutlinePage(i, page - 1, "lesson"),
                };
            }),
        });

        private sealed class Logging(ILlmClient inner, FakeAiCallStore calls) : ILlmClient
        {
            public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
            {
                var response = await inner.CompleteJsonAsync(request, cancellationToken);
                var id = Guid.NewGuid();
                await calls.AddAsync(new AiCall { Id = id, Purpose = request.Prompt.Id, Model = response.Model, PromptVersion = 1, CreatedAt = Now }, cancellationToken);
                return response with { AiCallId = id };
            }
        }
    }
}
