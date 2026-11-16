using System.Globalization;
using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// The body values of the WhatsApp templates Monika submitted (D-98, T-104; wording in <c>docs/whatsapp-setup.md</c> §3),
/// built by code from the curriculum in the family's language: never by the LLM (guardrail 4). Both templates say
/// "Nalikali Maths" in their fixed text. A Nalikali page is filled as it is; a page in the Maths textbook gets the book
/// after it, "80 (Maths textbook)", so the family is pointed to the right book (T-132, D-119). A lesson in any other
/// book, or without a name in the family's language, gives null and the message is not sent. Pure.
/// </summary>
public static class TemplateValues
{
    /// <summary>
    /// The word for page(s) and for "and", per template language, as in §3; and the Maths textbook's name with the word
    /// for its part (the evening wording's, D-99, with "Maths" added).
    /// </summary>
    private sealed record Words(string Page, string Pages, string And, string Textbook, string Part);

    private static readonly Dictionary<string, Words> ByLanguage = new(StringComparer.Ordinal)
    {
        ["en"] = new("page", "pages", " and ", "Maths textbook", "Part"),
        ["kn"] = new("ಪುಟ", "ಪುಟ", " ಮತ್ತು ", "ಗಣಿತ ಪಠ್ಯಪುಸ್ತಕ", "ಭಾಗ"),
        ["te"] = new("పేజీ", "పేజీలు", ", ", "గణిత పాఠ్యపుస్తకం", "భాగం"),
        ["ta"] = new("பக்கம்", "பக்கங்கள்", ", ", "கணிதப் பாடநூல்", "பகுதி"),
        ["hi"] = new("पृष्ठ", "पृष्ठ", " और ", "गणित पाठ्यपुस्तक", "भाग"),
    };

    private const string Textbook = "textbook";

    /// <summary>
    /// <c>todays_lesson_link_v1</c>: {{1}} child, {{2}} the lesson's names, {{3}} the first lesson's page number (the
    /// template's text already says "Nalikali Maths page"), with its book after it when that is the textbook, or null.
    /// </summary>
    public static IReadOnlyList<string>? Todays(
        string childName, string language, IReadOnlyList<string> competencyIds, CurriculumGraph curriculum)
    {
        if (Lessons(language, competencyIds, curriculum) is not { } lessons)
        {
            return null;
        }

        var first = lessons.Pages[0];
        return [childName, lessons.Names, Number(first.Page) + BookAfter(first, ByLanguage[language])];
    }

    /// <summary>
    /// <c>catchup_link_v1</c>: {{1}} child, {{2}} the target lessons' names ("A and B"), {{3}} their pages with the word
    /// ("page 4", "pages 4 and 6"), or null.
    /// </summary>
    public static IReadOnlyList<string>? CatchUp(
        string childName, string language, IReadOnlyList<string> targetIds, CurriculumGraph curriculum)
    {
        if (Lessons(language, targetIds, curriculum) is not { } lessons)
        {
            return null;
        }

        // Pages grouped by book: Nalikali's first, as the template's text names it, then the textbook's by part.
        var words = ByLanguage[language];
        var groups = lessons.Pages
            .GroupBy(p => (p.Book, Part: p.Book == TextbookPage.Nalikali ? null : p.Part))
            .OrderBy(g => g.Key.Book == TextbookPage.Nalikali ? 0 : 1)
            .ThenBy(g => g.Key.Part)
            .Select(g =>
            {
                var pages = g.Select(p => p.Page).Distinct().Order().Select(Number).ToList();
                return $"{(pages.Count == 1 ? words.Page : words.Pages)} {Join(pages, words.And)}{BookAfter(g.First(), words)}";
            })
            .ToList();
        return [childName, lessons.Names, Join(groups, words.And)];
    }

    /// <summary>The names joined and the page of each lesson in order; null if any is missing or in another book.</summary>
    private static (string Names, IReadOnlyList<TextbookPage> Pages)? Lessons(
        string language, IReadOnlyList<string> competencyIds, CurriculumGraph curriculum)
    {
        if (!ByLanguage.TryGetValue(language, out var words) || competencyIds.Count == 0)
        {
            return null;
        }

        var names = new List<string>();
        var pages = new List<TextbookPage>();
        foreach (var id in competencyIds)
        {
            if (curriculum.Get(id) is not { } lesson
                || lesson.Name(language) is not { } name
                || lesson.TextbookPage is not { Book: TextbookPage.Nalikali or Textbook } page)
            {
                return null;
            }

            names.Add(name);
            pages.Add(page);
        }

        return (Join(names, words.And), pages);
    }

    /// <summary>
    /// Nothing for a Nalikali page (the template says "Nalikali Maths"); " (Maths textbook)" for the textbook, with its
    /// part from part 2 on: " (Maths textbook Part 2)".
    /// </summary>
    private static string BookAfter(TextbookPage page, Words words) =>
        page.Book == TextbookPage.Nalikali
            ? ""
            : page.Part is > 1 and var part
                ? $" ({words.Textbook} {words.Part} {Number(part)})"
                : $" ({words.Textbook})";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>"A", "A and B", "A, B and C" (with the language's "and"; Telugu and Tamil use a comma).</summary>
    private static string Join(IReadOnlyList<string> items, string and) =>
        items.Count == 1 ? items[0] : $"{string.Join(", ", items.Take(items.Count - 1))}{and}{items[^1]}";
}
