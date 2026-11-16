namespace SlateAi.Api.Features.LessonContents;

/// <summary>A lesson's description and worked examples, ready to store (T-141).</summary>
public sealed record LessonContentText(string Description, IReadOnlyList<string> Examples);

/// <summary>
/// What a lesson's description and examples may be (T-141, D-123): a description of at most <see cref="MaxDescription"/>
/// characters and 1 to <see cref="MaxExamples"/> examples of at most <see cref="MaxExample"/> each, no braces (they would
/// look like a placeholder to A9). Pure; the admin page and the startup load use the same rules.
/// </summary>
public static class LessonContentRules
{
    public const int MaxDescription = 700;
    public const int MaxExamples = 4;
    public const int MaxExample = 200;

    /// <summary>
    /// The content to store, trimmed with blank examples dropped; null with no errors when everything is blank (remove
    /// it); otherwise the errors by field, for the admin page.
    /// </summary>
    public static (LessonContentText? Content, Dictionary<string, string[]> Errors) Normalise(
        string? description, IReadOnlyList<string?>? examples)
    {
        var errors = new Dictionary<string, string[]>();
        var text = description?.Trim() ?? "";
        var lines = (examples ?? []).Select(e => e?.Trim() ?? "").Where(e => e.Length > 0).ToList();
        if (text.Length == 0 && lines.Count == 0)
        {
            return (null, errors);
        }

        if (text.Length == 0)
        {
            errors["description"] = ["Write a description."];
        }
        else if (text.Length > MaxDescription)
        {
            errors["description"] = [$"At most {MaxDescription} characters."];
        }
        else if (HasBraces(text))
        {
            errors["description"] = ["No { or }."];
        }

        if (lines.Count == 0)
        {
            errors["examples"] = ["Write at least one example."];
        }
        else if (lines.Count > MaxExamples)
        {
            errors["examples"] = [$"At most {MaxExamples} examples."];
        }
        else if (lines.Any(l => l.Length > MaxExample))
        {
            errors["examples"] = [$"At most {MaxExample} characters each."];
        }
        else if (lines.Any(HasBraces))
        {
            errors["examples"] = ["No { or }."];
        }

        return errors.Count > 0 ? (null, errors) : (new LessonContentText(text, lines), errors);
    }

    private static bool HasBraces(string text) => text.Contains('{') || text.Contains('}');
}
