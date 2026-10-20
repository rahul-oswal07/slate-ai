namespace SlateAi.Api.Curriculum;

/// <summary>One node of the curriculum graph, e.g. "M3.ADD.CARRY". Loaded from <c>data/curriculum/*.json</c>.</summary>
public sealed record Competency(
    string Id,
    string NameKn,
    string NameEn,
    IReadOnlyList<string> Prerequisites,
    string? NipunOutcome,
    int Chapter,
    int Sequence,
    IReadOnlyList<string> Examples,
    TextbookPage? TextbookPage);

/// <summary>Where the child finds this in their own book (D-22). Structured so code can word it in any language.</summary>
/// <param name="Book">"textbook" or "workbook".</param>
public sealed record TextbookPage(string Book, int? Part, int Page);

/// <summary>Shape of one curriculum file.</summary>
public sealed record CurriculumFile(int Grade, string Subject, string? Version, IReadOnlyList<Competency> Competencies);
