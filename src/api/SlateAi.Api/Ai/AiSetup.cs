using Anthropic;
using Microsoft.Extensions.Options;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Ai;

public static class AiSetup
{
    /// <summary>Claude + Sarvam, or fakes when <c>Ai:Provider=Fake</c>. Both are wrapped so every call is logged.</summary>
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.Section));
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.Section));
        services.Configure<SarvamOptions>(configuration.GetSection(SarvamOptions.Section));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var directory = Path.Combine(AppContext.BaseDirectory, options.PromptsDirectory);
            return PromptLibrary.LoadDirectory(directory, options.Models.Keys);
        });

        services.AddSingleton(sp =>
        {
            var key = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value.ApiKey;
            // Without a key the client is still built; ClaudeLlmClient refuses calls (NotConfigured).
            return string.IsNullOrWhiteSpace(key) ? new AnthropicClient() : new AnthropicClient { ApiKey = key };
        });
        services.AddSingleton<ClaudeLlmClient>();
        services.AddSingleton<FakeLlmClient>();
        services.AddHttpClient<SarvamSpeechToText>(http => http.Timeout = TimeSpan.FromSeconds(30));
        services.AddSingleton<FakeSpeechToText>();
        services.AddHttpClient<SarvamTextToSpeech>(http => http.Timeout = TimeSpan.FromSeconds(60));
        services.AddSingleton<FakeTextToSpeech>();
        services.AddScoped<ITextToSpeech>(sp => new LoggedTextToSpeech(
            UseFakes(sp) ? sp.GetRequiredService<FakeTextToSpeech>() : sp.GetRequiredService<SarvamTextToSpeech>(),
            sp.GetRequiredService<IAiCallStore>(),
            sp.GetRequiredService<IOptions<AiOptions>>(),
            sp.GetRequiredService<IOptions<SarvamOptions>>(),
            sp.GetRequiredService<TimeProvider>()));
        services.AddScoped<IAiCallStore, AiCallStore>();

        services.AddScoped<ILlmClient>(sp => new LoggedLlmClient(
            UseFakes(sp) ? sp.GetRequiredService<FakeLlmClient>() : sp.GetRequiredService<ClaudeLlmClient>(),
            sp.GetRequiredService<IAiCallStore>(),
            sp.GetRequiredService<IOptions<AiOptions>>(),
            sp.GetRequiredService<TimeProvider>()));
        services.AddScoped<ISpeechToText>(sp => new LoggedSpeechToText(
            UseFakes(sp) ? sp.GetRequiredService<FakeSpeechToText>() : sp.GetRequiredService<SarvamSpeechToText>(),
            sp.GetRequiredService<IAiCallStore>(),
            sp.GetRequiredService<IOptions<AiOptions>>(),
            sp.GetRequiredService<IOptions<SarvamOptions>>(),
            sp.GetRequiredService<TimeProvider>()));
        return services;
    }

    /// <summary>Loads prompts now (a bad file stops startup) and warns about missing keys.</summary>
    public static void CheckAiSetup(this WebApplication app)
    {
        var prompts = app.Services.GetRequiredService<PromptLibrary>();
        var logger = app.Services.GetRequiredService<ILogger<PromptLibrary>>();
        logger.LogInformation("Loaded {Count} prompts", prompts.All.Count);

        var ai = app.Services.GetRequiredService<IOptions<AiOptions>>().Value;
        if (ai.UseFakes)
        {
            logger.LogWarning("Ai:Provider=Fake: Claude and Sarvam are replaced by fakes");
            return;
        }

        if (string.IsNullOrWhiteSpace(app.Services.GetRequiredService<IOptions<AnthropicOptions>>().Value.ApiKey))
        {
            logger.LogWarning("Anthropic:ApiKey is not set; AI features fall back to rules and the teacher");
        }

        if (string.IsNullOrWhiteSpace(app.Services.GetRequiredService<IOptions<SarvamOptions>>().Value.ApiKey))
        {
            logger.LogWarning("Sarvam:ApiKey is not set; voice capture falls back to the lesson picker and catch-ups get no audio");
        }
    }

    private static bool UseFakes(IServiceProvider sp) => sp.GetRequiredService<IOptions<AiOptions>>().Value.UseFakes;
}
