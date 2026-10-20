namespace SlateAi.Api.Ai;

/// <summary>A versioned system prompt from <c>prompts/&lt;id&gt;.md</c>. The version is logged with every call.</summary>
public sealed record Prompt(string Id, int Version, string ModelKey, string Text);

public sealed class PromptException(IReadOnlyList<string> errors)
    : Exception("Invalid prompts:" + Environment.NewLine + string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>Prompt files loaded once at startup. A bad header stops the app.</summary>
public sealed class PromptLibrary
{
    private readonly Dictionary<string, Prompt> _byId;

    private PromptLibrary(IEnumerable<Prompt> prompts) =>
        _byId = prompts.ToDictionary(p => p.Id, StringComparer.Ordinal);

    public IReadOnlyCollection<Prompt> All => _byId.Values;

    public Prompt Get(string id) =>
        _byId.TryGetValue(id, out var prompt)
            ? prompt
            : throw new InvalidOperationException($"No prompt file prompts/{id}.md.");

    public static PromptLibrary LoadDirectory(string directory, IReadOnlyCollection<string> modelKeys)
    {
        var files = Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.md")
                .Where(path => !Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .Select(path => (Path.GetFileName(path), File.ReadAllText(path)))
            : [];
        return Load(files, modelKeys);
    }

    /// <param name="files">(file name, content)</param>
    /// <param name="modelKeys">Keys configured under <c>Ai:Models</c>; a prompt must name one of them.</param>
    public static PromptLibrary Load(IEnumerable<(string Name, string Content)> files, IReadOnlyCollection<string> modelKeys)
    {
        var errors = new List<string>();
        var prompts = new List<Prompt>();
        foreach (var (name, content) in files)
        {
            var fileErrors = new List<string>();
            var (header, body) = Split(content);
            if (header is null)
            {
                errors.Add($"{name}: missing '---' header with id, version and model_key");
                continue;
            }

            var id = header.GetValueOrDefault("id");
            if (id != Path.GetFileNameWithoutExtension(name))
            {
                fileErrors.Add($"id must match the file name ({Path.GetFileNameWithoutExtension(name)})");
            }

            if (!int.TryParse(header.GetValueOrDefault("version"), out var version) || version < 1)
            {
                fileErrors.Add("version must be a positive whole number");
            }

            var modelKey = header.GetValueOrDefault("model_key") ?? "";
            if (!modelKeys.Contains(modelKey))
            {
                fileErrors.Add($"model_key '{modelKey}' is not configured under Ai:Models");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                fileErrors.Add("prompt text is empty");
            }

            errors.AddRange(fileErrors.Select(e => $"{name}: {e}"));
            if (fileErrors.Count == 0)
            {
                prompts.Add(new Prompt(id!, version, modelKey, body.Trim()));
            }
        }

        return errors.Count > 0 ? throw new PromptException(errors) : new PromptLibrary(prompts);
    }

    private static (Dictionary<string, string>? Header, string Body) Split(string content)
    {
        var lines = content.ReplaceLineEndings("\n").Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            return (null, content);
        }

        var end = Array.FindIndex(lines, 1, l => l.Trim() == "---");
        if (end < 0)
        {
            return (null, content);
        }

        var header = lines[1..end]
            .Select(l => l.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.Ordinal);
        return (header, string.Join('\n', lines[(end + 1)..]));
    }
}
