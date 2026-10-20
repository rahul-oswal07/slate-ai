using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Ai;

public sealed class AiSetupTests
{
    [Fact]
    public async Task With_the_fake_provider_calls_are_answered_and_logged_without_keys()
    {
        var store = new FakeAiCallStore();
        await using var factory = FakeApi.Create(services => services.AddScoped<IAiCallStore>(_ => store));
        using var scope = factory.Services.CreateScope();
        var llm = scope.ServiceProvider.GetRequiredService<ILlmClient>();
        var stt = scope.ServiceProvider.GetRequiredService<ISpeechToText>();
        var schema = JsonDocument.Parse("""{ "type": "object", "properties": { "ok": { "type": "boolean" } } }""").RootElement;

        var response = await llm.CompleteJsonAsync(
            new LlmRequest(new Prompt("p", 1, "Mapping", "t"), "x", schema), TestContext.Current.CancellationToken);
        var transcription = await stt.TranscribeAsync(
            new MemoryStream([1]), "a.webm", "audio/webm", "kn-IN", [], TestContext.Current.CancellationToken);

        Assert.Equal("""{"ok":true}""", response.Json);
        Assert.Equal(FakeSpeechToText.Transcript, transcription.Text);
        Assert.Equal([response.AiCallId, transcription.AiCallId], store.Calls.Select(c => c.Id));
        Assert.Equal(["fake", "fake"], store.Calls.Select(c => c.Model));
    }

    [Fact]
    public async Task The_configured_models_come_from_appsettings()
    {
        await using var factory = FakeApi.Create();

        var options = factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;

        Assert.Equal("claude-sonnet-5-5", options.ModelFor("Mapping"));
        Assert.Equal("claude-opus-5-5", options.ModelFor("Script"));
        Assert.Equal("claude-haiku-5-5", options.ModelFor("Check"));
    }

    [Fact]
    public async Task An_invalid_prompt_file_stops_startup()
    {
        var directory = Directory.CreateTempSubdirectory("prompts-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "a1.md"), "no header", TestContext.Current.CancellationToken);
            await using var factory = FakeApi.Create(settings: new Dictionary<string, string?> { ["Ai:PromptsDirectory"] = directory });

            var error = Assert.Throws<PromptException>(() => factory.Services);

            Assert.Contains("a1.md: missing '---' header", Assert.Single(error.Errors));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
