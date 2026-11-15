using System.Globalization;
using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// The body values of the WhatsApp templates Monika submitted (D-98, T-104; wording in <c>docs/whatsapp-setup.md</c> §3),
/// built by code from the curriculum in the family's language: never by the LLM (guardrail 4). Both templates say
/// "Nalikali Maths" in their fixed text, so values are built only when every page is a Nalikali page; anything else gives
/// null and the message is not sent, rather than sending a family to the wrong book. Pure.
/// </summary>
public static class TemplateValues
{
    /// <summary>The word for page(s) and for "and", per template language, as in §3.</summary>
    private sealed record Words(string Page, string Pages, string And);

    private static readonly Dictionary<string, Words> ByLanguage = new(StringComparer.Ordinal)
    {
        ["en"] = new("page", "pages", " and "),
        ["kn"] = new("ಪುಟ", "ಪುಟ", " ಮತ್ತು "),
        ["te"] = new("పేజీ", "పేజీలు", ", "),
        ["ta"] = new("பக்கம்", "பக்கங்கள்", ", "),
        ["hi"] = new("पृष्ठ", "पृष्ठ", " और "),
    };

    /// <summary>
    /// <c>todays_lesson_link_v1</c>: {{1}} child, {{2}} the lesson's names, {{3}} the first lesson's page number (the
    /// template's text already says "page"), or null.
    /// </summary>
    public static IReadOnlyList<string>? Todays(
        string childName, string language, IReadOnlyList<string> competencyIds, CurriculumGraph curriculum) =>
        Lessons(language, competencyIds, curriculum) is { } lessons
            ? [childName, lessons.Names, lessons.Pages[0].ToString(CultureInfo.InvariantCulture)]
            : null;

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

        var words = ByLanguage[language];
        var pages = lessons.Pages.Distinct().Order().Select(p => p.ToString(CultureInfo.InvariantCulture)).ToList();
        var pageText = $"{(pages.Count == 1 ? words.Page : words.Pages)} {Join(pages, words.And)}";
        return [childName, lessons.Names, pageText];
    }

    /// <summary>The names joined and the Nalikali page of each lesson in order; null if any is missing.</summary>
    private static (string Names, IReadOnlyList<int> Pages)? Lessons(
        string language, IReadOnlyList<string> competencyIds, CurriculumGraph curriculum)
    {
        if (!ByLanguage.TryGetValue(language, out var words) || competencyIds.Count == 0)
        {
            return null;
        }

        var names = new List<string>();
        var pages = new List<int>();
        foreach (var id in competencyIds)
        {
            if (curriculum.Get(id) is not { } lesson
                || lesson.Name(language) is not { } name
                || lesson.TextbookPage is not { Book: TextbookPage.Nalikali } page)
            {
                return null;
            }

            names.Add(name);
            pages.Add(page.Page);
        }

        return (Join(names, words.And), pages);
    }

    /// <summary>"A", "A and B", "A, B and C" (with the language's "and"; Telugu and Tamil use a comma).</summary>
    private static string Join(IReadOnlyList<string> items, string and) =>
        items.Count == 1 ? items[0] : $"{string.Join(", ", items.Take(items.Count - 1))}{and}{items[^1]}";
}
