namespace SlateAi.Api.Ai;

public sealed class AiOptions
{
    public const string Section = "Ai";

    /// <summary>"Claude" (default) or "Fake" (no keys needed; local UI work and tests).</summary>
    public string Provider { get; set; } = "Claude";

    /// <summary>Model ID per prompt <c>model_key</c>, e.g. Mapping → claude-sonnet-5-5 (guardrail 5).</summary>
    public Dictionary<string, string> Models { get; set; } = [];

    /// <summary>Folder of prompt files, relative to the app's output directory.</summary>
    public string PromptsDirectory { get; set; } = "prompts";

    public bool UseFakes => string.Equals(Provider, "Fake", StringComparison.OrdinalIgnoreCase);

    public string ModelFor(string modelKey) =>
        Models.TryGetValue(modelKey, out var model) && !string.IsNullOrWhiteSpace(model)
            ? model
            : throw new LlmException(LlmFailure.NotConfigured, $"No model configured for Ai:Models:{modelKey}.");
}

public sealed class AnthropicOptions
{
    public const string Section = "Anthropic";

    /// <summary>From user-secrets or the environment only, never appsettings.</summary>
    public string? ApiKey { get; set; }
}
