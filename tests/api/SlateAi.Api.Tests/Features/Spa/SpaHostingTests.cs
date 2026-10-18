using System.Net;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Spa;

[Trait("Category", "Integration")]
public sealed class SpaHostingTests(PostgresFixture postgres) : IDisposable
{
    private const string IndexMarker = "<!-- spa-index -->";
    private readonly string _webRoot = CreateWebRoot();

    [Theory]
    [InlineData("/")]
    [InlineData("/ledger/123")]
    public async Task Client_routes_serve_index_html(string path)
    {
        await using var factory = new ApiFactory(postgres.ConnectionString, webRoot: _webRoot);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(IndexMarker, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Unknown_api_route_returns_404_not_html()
    {
        await using var factory = new ApiFactory(postgres.ConnectionString, webRoot: _webRoot);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(IndexMarker, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/sw.js")]
    [InlineData("/index.html")]
    [InlineData("/")]
    public async Task Service_worker_and_shell_are_not_cached(string path)
    {
        await using var factory = new ApiFactory(postgres.ConnectionString, webRoot: _webRoot);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoCache, $"{path} should be served with Cache-Control: no-cache");
    }

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    private static string CreateWebRoot()
    {
        var root = Directory.CreateTempSubdirectory("slateai-webroot-").FullName;
        File.WriteAllText(Path.Combine(root, "index.html"), $"<!doctype html><html>{IndexMarker}</html>");
        File.WriteAllText(Path.Combine(root, "sw.js"), "// service worker stub");
        return root;
    }
}
