using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.TextbookImport;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.TextbookImport;

/// <summary>T-146: upload a textbook and follow its import on the admin page.</summary>
public sealed class TextbookImportEndpointTests
{
    private const string Url = "/api/admin/curriculum-imports";
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly FakeTextbookImportStore _store = new();
    private readonly MovableClock _clock = new() { Now = Now };

    [Fact]
    public async Task An_upload_is_split_into_pages_queued_and_a_replay_returns_the_same_import()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var requestId = Guid.NewGuid();

        var first = await client.PostAsync(Url, Form(requestId, PdfPagesTests.Book(4)), Ct);
        var replay = await client.PostAsync(Url, Form(requestId, PdfPagesTests.Book(4)), Ct);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        var view = (await first.Content.ReadFromJsonAsync<TextbookImportView>(Ct))!;
        Assert.Equal((CurriculumImportStatuses.Queued, 4, "maths", "textbook", (int?)1), (view.Status, view.PageCount, view.Subject, view.Book, view.Part));
        Assert.Equal(view.Id, (await replay.Content.ReadFromJsonAsync<TextbookImportView>(Ct))!.Id);
        Assert.Single(_store.Imports);
        Assert.Equal(4, _store.Pages[view.Id].Count);
        Assert.Equal(view.Id, await NextJobAsync(factory));
        Assert.Equal("no-store", first.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Missing_fields_a_file_that_is_not_a_pdf_and_a_pdf_that_can_t_be_read_are_refused_with_nothing_stored()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var missing = await client.PostAsync(Url, Form(Guid.NewGuid(), PdfPagesTests.Book(1), grade: "7", subject: "Maths 2", book: "novel"), Ct);
        var notPdf = await client.PostAsync(Url, Form(Guid.NewGuid(), Encoding.UTF8.GetBytes("hello"), contentType: "text/plain"), Ct);
        var broken = await client.PostAsync(Url, Form(Guid.NewGuid(), Encoding.UTF8.GetBytes("%PDF-1.7 broken")), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var errors = await missing.Content.ReadAsStringAsync(Ct);
        Assert.Contains("\"grade\"", errors);
        Assert.Contains("\"subject\"", errors);
        Assert.Contains("\"book\"", errors);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, notPdf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, broken.StatusCode);
        Assert.Contains("can't be read", await broken.Content.ReadAsStringAsync(Ct));
        Assert.Empty(_store.Imports);
    }

    [Fact]
    public async Task The_list_shows_progress_and_an_import_that_stopped_moving_reads_as_failed_and_can_be_tried_again()
    {
        var stuck = _store.Add(TextbookStepsTests.Book, [[1]], Now.AddMinutes(-30));
        _store.Imports[stuck.Id].Status = CurriculumImportStatuses.Running;
        _store.Imports[stuck.Id].Stage = CurriculumImportStages.Lessons;
        var ready = _store.Add(TextbookStepsTests.Book with { Title = "Part 2" }, [[1]], Now);
        _store.Imports[ready.Id].Status = CurriculumImportStatuses.Ready;
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var list = (await client.GetFromJsonAsync<List<TextbookImportView>>(Url, Ct))!;
        var retried = await client.PostAsJsonAsync($"{Url}/{stuck.Id}/retry", new RetryImportRequest(Guid.NewGuid()), Ct);
        var readyRetry = await client.PostAsJsonAsync($"{Url}/{ready.Id}/retry", new RetryImportRequest(Guid.NewGuid()), Ct);
        var unknown = await client.PostAsJsonAsync($"{Url}/{Guid.NewGuid()}/retry", new RetryImportRequest(Guid.NewGuid()), Ct);

        Assert.Equal(["Part 2", "Grade 3 maths"], list.Select(i => i.Title));
        Assert.Equal((CurriculumImportStatuses.Failed, "error:stale", (string?)null), (list[1].Status, list[1].Error, list[1].Stage));
        Assert.Equal(CurriculumImportStatuses.Queued, (await retried.Content.ReadFromJsonAsync<TextbookImportView>(Ct))!.Status);
        Assert.Equal(stuck.Id, await NextJobAsync(factory));
        Assert.Equal(CurriculumImportStatuses.Ready, (await readyRetry.Content.ReadFromJsonAsync<TextbookImportView>(Ct))!.Status);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task A_ready_import_shows_its_draft_counts_and_notes_and_discard_deletes_it_and_its_draft()
    {
        var import = _store.Add(TextbookStepsTests.Book, [[1]], Now);
        await _store.SaveDraftAsync(import.Id, new DraftCurriculum([], []), ["Chapter numbers were renumbered."], Now, Ct);
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var view = (await client.GetFromJsonAsync<TextbookImportView>($"{Url}/{import.Id}", Ct))!;
        var discard = await client.DeleteAsync($"{Url}/{import.Id}", Ct);
        var again = await client.DeleteAsync($"{Url}/{import.Id}", Ct);

        Assert.Equal((CurriculumImportStatuses.Ready, 0, 0), (view.Status, view.Lessons, view.Flagged));
        Assert.Equal(["Chapter numbers were renumbered."], view.Notes);
        Assert.Equal(HttpStatusCode.NoContent, discard.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Empty(_store.Imports);
        Assert.Single(_store.DiscardedDrafts);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{import.Id}", Ct)).StatusCode);
    }

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ITextbookImportStore>(_store);
            services.AddSingleton<TimeProvider>(_clock);
            // The test reads the queue itself instead of the worker running Claude.
            var worker = services.Single(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(TextbookImportWorker));
            services.Remove(worker);
        });

    private static async Task<Guid> NextJobAsync(ApiFactory factory)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        await foreach (var job in factory.Services.GetRequiredService<TextbookImportQueue>().ReadAllAsync(timeout.Token))
        {
            return job.ImportId;
        }

        throw new InvalidOperationException("Nothing was queued.");
    }

    private static MultipartFormDataContent Form(
        Guid requestId, byte[] pdf, string grade = "3", string subject = "maths", string book = "textbook",
        string contentType = "application/pdf")
    {
        var file = new ByteArrayContent(pdf);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent
        {
            { file, "pdf", "book.pdf" },
            { new StringContent(requestId.ToString()), "clientRequestId" },
            { new StringContent("Grade 3 maths, Part 1"), "title" },
            { new StringContent(grade), "grade" },
            { new StringContent(subject), "subject" },
            { new StringContent("kn"), "medium" },
            { new StringContent(book), "book" },
            { new StringContent("1"), "part" },
        };
    }
}
