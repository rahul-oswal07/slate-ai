using System.Net;
using System.Text.Json;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Docs;

[Trait("Category", "Integration")]
public sealed class ApiDocsTests(PostgresFixture postgres)
{
    private static readonly Dictionary<string, string?> DocsOn = new() { ["OpenApi:Enabled"] = "true" };

    [Fact]
    public async Task OpenApi_document_is_served_when_enabled()
    {
        await using var factory = new ApiFactory(postgres.ConnectionString, settings: DocsOn);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/openapi/v1.json", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        Assert.StartsWith("3.", root.GetProperty("openapi").GetString());
        Assert.Equal("Tappida Paatha API", root.GetProperty("info").GetProperty("title").GetString());

        // The catch-all /api 404 fallback is plumbing, not an endpoint.
        var paths = root.TryGetProperty("paths", out var p) ? p.EnumerateObject().Select(x => x.Name).ToList() : [];
        Assert.DoesNotContain(paths, path => path.StartsWith("/api/{", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Swagger_ui_is_served_when_enabled()
    {
        await using var factory = new ApiFactory(postgres.ConnectionString, settings: DocsOn);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/swagger/index.html", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task Docs_are_off_by_default()
    {
        await using var factory = new ApiFactory(postgres.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
