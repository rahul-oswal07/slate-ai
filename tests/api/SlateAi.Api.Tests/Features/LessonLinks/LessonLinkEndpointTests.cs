using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonLinks;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.LessonLinks;

/// <summary>The lesson links admin page (T-94, D-106).</summary>
public sealed class LessonLinkEndpointTests
{
    private const string Url = "/api/admin/lesson-links";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private static readonly CurriculumGraph Book = new(
        [
            new Competency("M3.BRIDGE.COMPARE", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "More, less, equal", [], null, 0, 0, [], new TextbookPage("nalikali", 1, 4)),
            new Competency("M3.SUB.BORROW", "ಎರವಲು", "Subtraction with borrowing", [], null, 5, 15, [], new TextbookPage("textbook", 2, 15)),
            new Competency("M3.SUB.MAKE", "ಸಮಸ್ಯಾ ರಚನೆ", "Making subtraction sums", [], null, 5, 14, [], null),
        ],
        [new Chapter(0, "ಸೇತುಬಂಧ", "Bridge course"), new Chapter(5, "ಕಳೆಯುವ ಮೋಜು", "The fun of subtracting")]);

    private readonly FakeLessonLinkStore _store = new();

    [Fact]
    public async Task Lists_every_chapter_and_lesson_in_book_order_with_its_link()
    {
        _store.Links["M3.SUB.BORROW"] = "https://diksha.gov.in/play/content/do_9";
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(Url, Ct);
        var body = (await response.Content.ReadFromJsonAsync<AdminLessonLinksResponse>(Ct))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal([0, 5], body.Chapters.Select(c => c.Number));
        Assert.Equal("The fun of subtracting", body.Chapters[1].NameEn);
        Assert.Equal(["M3.SUB.MAKE", "M3.SUB.BORROW"], body.Chapters[1].Lessons.Select(l => l.Id));
        Assert.Equal("https://diksha.gov.in/play/content/do_9", body.Chapters[1].Lessons[1].Url);
        Assert.Null(body.Chapters[0].Lessons[0].Url);
        Assert.Equal(4, body.Chapters[0].Lessons[0].TextbookPage!.Page);
    }

    [Fact]
    public async Task Sets_replaces_and_removes_a_link_and_a_replay_changes_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var first = Guid.NewGuid();

        var set = await PutAsync(client, first, "  https://diksha.gov.in/play/content/do_1 ");
        var replaced = await PutAsync(client, Guid.NewGuid(), "https://www.youtube.com/watch?v=abc");
        var replay = await PutAsync(client, first, "https://diksha.gov.in/play/content/do_1");

        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.Equal("https://diksha.gov.in/play/content/do_1", (await set.Content.ReadFromJsonAsync<AdminLessonLink>(Ct))!.Url);
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
        Assert.Equal("https://www.youtube.com/watch?v=abc", (await replay.Content.ReadFromJsonAsync<AdminLessonLink>(Ct))!.Url);
        Assert.Equal(2, _store.Writes);

        var removed = await PutAsync(client, Guid.NewGuid(), "");

        Assert.Null((await removed.Content.ReadFromJsonAsync<AdminLessonLink>(Ct))!.Url);
        Assert.Empty(_store.Links);
    }

    [Theory]
    [InlineData("http://diksha.gov.in/play/content/do_1")]
    [InlineData("diksha.gov.in")]
    [InlineData("javascript:alert(1)")]
    public async Task Anything_but_an_https_link_is_refused(string url)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await PutAsync(client, Guid.NewGuid(), url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"url\"", await response.Content.ReadAsStringAsync(Ct));
        Assert.Empty(_store.Links);
    }

    [Fact]
    public async Task A_lesson_not_in_the_curriculum_is_not_found()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{Url}/M3.NOPE", new SetLessonLinkRequest(Guid.NewGuid(), "https://diksha.gov.in/x"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _store.Writes);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task It_opens_and_saves_without_a_key_D112(string environment)
    {
        await using var factory = Create(environment);
        using var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PutAsync(client, Guid.NewGuid(), "https://diksha.gov.in/x")).StatusCode);
        Assert.Equal(1, _store.Writes);
    }

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid requestId, string? url) =>
        client.PutAsJsonAsync($"{Url}/M3.BRIDGE.COMPARE", new SetLessonLinkRequest(requestId, url), Ct);

    private ApiFactory Create(string environment = "Testing") => FakeApi.Create(
        services =>
        {
            services.AddSingleton<ILessonLinkStore>(_store);
            services.AddSingleton(new BuiltInCurriculum(Book));
        },
        environment: environment);
}
