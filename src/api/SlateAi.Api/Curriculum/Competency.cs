namespace SlateAi.Api.Curriculum;

/// <summary>One node of the curriculum graph, e.g. "M3.ADD.CARRY". Loaded from <c>data/curriculum/*.json</c>.</summary>
/// <param name="NameTe">Telugu name for parent-facing text (evening message); optional, draft until native review (T-26).</param>
/// <param name="NameTa">Tamil name, as <paramref name="NameTe"/>.</param>
/// <param name="KeyWordsKn">Textbook Kannada words for this lesson; A3 picks the catch-up's 3 key words from them (FR-07).</param>
/// <param name="Practice">How code makes this lesson's numbers (activity and learning check; guardrail 4).</param>
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
    string? NameTa = null,
    IReadOnlyList<string>? KeyWordsKn = null,
    PracticeRule? Practice = null)
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

/// <summary>
/// The kind of sum or question code generates for a lesson. Kinds: count, expanded, placeValue, biggest, smallest,
/// add, sub. Numbers follow the textbook: ≤ 999, no borrowing across a zero (docs/design/curriculum.md).
/// </summary>
/// <param name="Digits">2, 3 or 4.</param>
/// <param name="Carry">add: "none", "ones" (a single carry from the ones) or "any".</param>
/// <param name="Borrow">sub: "none" or "tens" (borrow a ten, never across a zero).</param>
public sealed record PracticeRule(string Kind, int Digits, string? Carry = null, string? Borrow = null)
{
    public static readonly string[] Kinds = ["count", "expanded", "placeValue", "biggest", "smallest", "add", "sub"];
}

/// <summary>A textbook chapter, for the lesson picker's titles ("Chapter 5 · The fun of subtracting"). 0 = bridge course.</summary>
public sealed record Chapter(int Number, string NameKn, string NameEn);

/// <summary>Shape of one curriculum file. When <see cref="Chapters"/> is given, every competency's chapter must be in it.</summary>
public sealed record CurriculumFile(
    int Grade, string Subject, string? Version, IReadOnlyList<Competency> Competencies, IReadOnlyList<Chapter>? Chapters = null);
