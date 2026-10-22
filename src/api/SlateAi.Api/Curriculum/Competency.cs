namespace SlateAi.Api.Curriculum;

/// <summary>One node of the curriculum graph, e.g. "M3.ADD.CARRY". Loaded from <c>data/curriculum/*.json</c>.</summary>
/// <param name="NameTe">Telugu name for parent-facing text (evening message); optional, draft until native review (T-26).</param>
/// <param name="NameTa">Tamil name, as <paramref name="NameTe"/>.</param>
public sealed record Competency(
    string Id,
    string NameKn,
    string NameEn,
    IReadOnlyList<string> Prerequisites,
    string? NipunOutcome,
    int Chapter,
    int Sequence,
    IReadOnlyList<string> Examples,
    TextbookPage? TextbookPage,
    string? NameTe = null,
    string? NameTa = null)
{
    /// <summary>The name in <paramref name="language"/> ("kn", "te", "ta", "en"), or null if there is none.</summary>
    public string? Name(string language)
    {
        var name = language switch
        {
            "kn" => NameKn,
            "en" => NameEn,
            "te" => NameTe,
            "ta" => NameTa,
            _ => null,
        };
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}

/// <summary>Where the child finds this in their own book (D-22). Structured so code can word it in any language.</summary>
/// <param name="Book">"textbook" or "workbook".</param>
public sealed record TextbookPage(string Book, int? Part, int Page);

/// <summary>A textbook chapter, for the lesson picker's titles ("Chapter 5 · The fun of subtracting"). 0 = bridge course.</summary>
public sealed record Chapter(int Number, string NameKn, string NameEn);

/// <summary>Shape of one curriculum file. When <see cref="Chapters"/> is given, every competency's chapter must be in it.</summary>
public sealed record CurriculumFile(
    int Grade, string Subject, string? Version, IReadOnlyList<Competency> Competencies, IReadOnlyList<Chapter>? Chapters = null);
