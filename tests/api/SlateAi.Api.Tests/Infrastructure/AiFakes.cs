using System.Net;
using System.Text;
using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>Answers HTTP requests with canned responses and keeps a copy of each request (body read eagerly).</summary>
public sealed class StubHttpHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
{
    private int _next;

    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return responses[Math.Min(_next++, responses.Length - 1)]();
    }
}

public sealed class FakeAiCallStore : IAiCallStore
{
    public List<AiCall> Calls { get; } = [];

    public Task AddAsync(AiCall call, CancellationToken cancellationToken)
    {
        Calls.Add(call);
        return Task.CompletedTask;
    }

    public Task SetVerdictAsync(Guid id, string verdict, CancellationToken cancellationToken)
    {
        Calls.Single(c => c.Id == id).Verdict = verdict;
        return Task.CompletedTask;
    }
}

/// <summary>Fails the test if anything calls an LLM: for paths with no AI (evening message FR-20, the parent page).</summary>
public sealed class ThrowingLlmClient : ILlmClient
{
    public Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("This path must not call an LLM (FR-20, FR-21, guardrail 3).");
}
