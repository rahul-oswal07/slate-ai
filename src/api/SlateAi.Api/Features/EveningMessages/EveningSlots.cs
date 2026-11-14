using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.EveningMessages;

/// <summary>A run of the message: fixed text, or a slot filled by code (the T0 example underlines slots).</summary>
/// <param name="Slot">"child", "lesson" or "page"; null for fixed text.</param>
public sealed record TextPart(string Text, string? Slot);

/// <summary>Slot values stored with a sent message. The child's name is not stored here.</summary>
public sealed record SlotValues(string Lesson, string Page);

public sealed record FilledMessage(string Text, IReadOnlyList<TextPart> Parts, SlotValues Slots);

/// <summary>
/// Fills the approved evening template (FR-20, D-21). Pure code, no LLM: <c>{child}</c> is the first name (written
/// text only, D-50), <c>{lesson}</c> the confirmed lessons' names in the family's language, <c>{page}</c> the first
/// lesson's page with its book's name ("Nalikali Maths Part 1, page 4"; T-101, D-99). Anything missing gives null: a
/// message is never sent half-filled.
/// </summary>
public static class EveningSlots
{
    private const string BookSlot = "{book}";

    public static FilledMessage? Fill(
        LanguageWording wording,
        string language,
        string childName,
        IReadOnlyList<string> competencyIds,
        CurriculumGraph curriculum)
    {
        var lessons = competencyIds.Select(curriculum.Get).ToList();
        if (lessons.Count == 0 || lessons.Any(l => l is null))
        {
            return null;
        }

        var names = lessons.Select(l => l!.Name(language)).ToList();
        // Homework is in the child's own book: the textbook or Nalikali (D-90), never a workbook page.
        if (names.Any(n => n is null) || lessons[0]!.TextbookPage is not { Book: "textbook" or TextbookPage.Nalikali } page)
        {
            return null;
        }

        var format = page.Part is null ? wording.PageNoPart : wording.Page;
        if (format.Contains(BookSlot, StringComparison.Ordinal))
        {
            // Wording v4 (T-101, D-99): code names the book, so a Nalikali page never reads "textbook". Wording
            // approved before it has no {book} and keeps "textbook" in its text.
            if (wording.Books?.GetValueOrDefault(page.Book) is not { Length: > 0 } bookName)
            {
                return null;
            }

            format = format.Replace(BookSlot, bookName);
        }

        var pageText = format.Replace("{part}", page.Part?.ToString()).Replace("{page}", page.Page.ToString());
        var slots = new SlotValues(string.Join(", ", names), pageText);
        return Render(wording.Text, childName, slots);
    }

    /// <summary>Puts the values into <paramref name="text"/>; also rebuilds a sent message from its stored slots.</summary>
    public static FilledMessage Render(string text, string childName, SlotValues slots)
    {
        var values = new Dictionary<string, string>
        {
            ["child"] = childName,
            ["lesson"] = slots.Lesson,
            ["page"] = slots.Page,
        };

        var parts = new List<TextPart>();
        var at = 0;
        while (at < text.Length)
        {
            var open = text.IndexOf('{', at);
            var close = open < 0 ? -1 : text.IndexOf('}', open);
            var slot = close < 0 ? null : text[(open + 1)..close];
            if (slot is null || !values.ContainsKey(slot))
            {
                // No more slots (an unknown "{x}" stays as written).
                var end = close < 0 ? text.Length : close + 1;
                Append(parts, text[at..end], null);
                at = end;
                continue;
            }

            Append(parts, text[at..open], null);
            parts.Add(new TextPart(values[slot], slot));
            at = close + 1;
        }

        return new FilledMessage(string.Concat(parts.Select(p => p.Text)), parts, slots);
    }

    private static void Append(List<TextPart> parts, string text, string? slot)
    {
        if (text.Length == 0)
        {
            return;
        }

        if (slot is null && parts.Count > 0 && parts[^1].Slot is null)
        {
            parts[^1] = parts[^1] with { Text = parts[^1].Text + text };
            return;
        }

        parts.Add(new TextPart(text, slot));
    }
}
