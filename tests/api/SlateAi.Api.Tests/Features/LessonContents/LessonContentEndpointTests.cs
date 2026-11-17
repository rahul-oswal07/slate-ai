using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.LessonLinks;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.LessonContents;

/// <summary>A lesson's description and examples on /admin/lesson-links (T-141, D-123).</summary>
public sealed class LessonContentEndpointTests
{
    private const string Url = "/api/admin/lesson-content";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private static readonly CurriculumGraph Book = new(
        [
            new Competency("M3.BRIDGE.COMPARE", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "More, less, equal", [], null, 0, 0, [], new TextbookPage("nalikali", 1, 4)),
            new Competency("M3.SUB.BORROW", "ಎರವಲು", "Subtraction with borrowing", [], null, 4, 23, [], new TextbookPage("textbook", 1, 108)),
        ],
        [new Chapter(0, "ಸೇತುಬಂಧ", "Bridge course"), new Chapter(4, "ವ್ಯವಕಲನ", "Subtraction")]);

    private readonly FakeLessonContentStore _store = new();

    [Fact]
    public async Task The_lesson_list_carries_each_lessons_description_and_examples()
    {
        _store.Contents["M3.SUB.BORROW"] = new StoredLessonContent("Open a bundle of ten.", ["52 − 18: open a ten."], "textbook");
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var body = (await client.GetFromJsonAsync<AdminLessonLinksResponse>("/api/admin/lesson-links", Ct))!;

        var borrow = body.Chapters[1].Lessons.Single().Content!;
        Assert.Equal(("Open a bundle of ten.", "textbook"), (borrow.Description, borrow.Source));
        Assert.Equal(["52 − 18: open a ten."], borrow.Examples);
        Assert.Null(body.Chapters[0].Lessons.Single().Content);
    }

    [Fact]
    public async Task Sets_trims_and_removes_content_marks_it_edited_and_a_replay_changes_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var first = Guid.NewGuid();

        var set = await PutAsync(client, first, "  Look at the tens first. ", [" 52 and 25: 52 is more. ", "", null]);
        var replay = await PutAsync(client, first, "Something else", ["Other"]);

        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        var saved = (await set.Content.ReadFromJsonAsync<AdminLessonContent>(Ct))!;
        Assert.Equal(("Look at the tens first.", "edited"), (saved.Description, saved.Source));
        Assert.Equal(["52 and 25: 52 is more."], saved.Examples);
        Assert.Equal("Look at the tens first.", (await replay.Content.ReadFromJsonAsync<AdminLessonContent>(Ct))!.Description);
        Assert.Equal(1, _store.Writes);

        var removed = await PutAsync(client, Guid.NewGuid(), " ", []);

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Empty(_store.Contents);
    }

    [Theory]
    [InlineData("", "an example", "description")]
    [InlineData("A description", "", "examples")]
    [InlineData("Hello {child}", "an example", "description")]
    public async Task Content_that_breaks_the_rules_is_refused_and_says_which_field(string description, string example, string field)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await PutAsync(client, Guid.NewGuid(), description, [example]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains($"\"{field}\"", await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal(0, _store.Writes);
    }

    [Fact]
    public async Task A_lesson_not_in_the_curriculum_is_not_found()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync(
            $"{Url}/M3.NOPE", new SetLessonContentRequest(Guid.NewGuid(), "A description", ["An example"]), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _store.Writes);
    }

    [Fact]
    public async Task It_saves_without_a_key_on_production_D112()
    {
        await using var factory = Create("Production");
        using var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.OK, (await PutAsync(client, Guid.NewGuid(), "A description", ["An example"])).StatusCode);
    }

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid requestId, string? description, IReadOnlyList<string?> examples) =>
        client.PutAsJsonAsync($"{Url}/M3.BRIDGE.COMPARE", new SetLessonContentRequest(requestId, description, examples), Ct);

    private ApiFactory Create(string environment = "Testing") => FakeApi.Create(
        services =>
        {
            services.AddSingleton<ILessonContentStore>(_store);
            services.AddSingleton(new BuiltInCurriculum(Book));
        },
        environment: environment);
}
