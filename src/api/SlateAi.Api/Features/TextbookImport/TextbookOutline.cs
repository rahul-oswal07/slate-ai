using System.Text.Json;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>
/// Stage 1 of the import (T-146): A10 reads a few pages at a time and says what each page is; code checks it and works
/// out the chapters and their pages. Pure.
/// </summary>
public static class TextbookOutline
{
    public const string PromptId = "a10-textbook-outline";

    private const int MaxTitle = 100;
    private const int MaxPrintedPage = 2000;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static JsonElement Schema() => JsonSchema.ToElement(JsonSchema.Object(
        ("pages", JsonSchema.Array(JsonSchema.Object(
            ("page", JsonSchema.Integer()),
            ("printedPage", JsonSchema.Integer()),
            ("kind", JsonSchema.Enum(PageKinds.All)),
            ("chapterStarts", JsonSchema.Boolean()),
            ("chapterNumber", JsonSchema.Integer()),
            ("chapterTitleKn", JsonSchema.String()),
            ("chapterTitleEn", JsonSchema.String()))))));

    /// <summary>The window's pages as A10 read them, or null when the output can't be used.</summary>
    /// <param name="firstPage">The window's first PDF page; A10 numbers the window's pages from 1.</param>
    /// <param name="count">Pages in the window. A page A10 left out is kept as <see cref="PageKinds.Other"/>.</param>
    public static IReadOnlyList<OutlinePage>? Validate(string json, int firstPage, int count)
    {
        OutlineOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<OutlineOutput>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (output?.Pages is null)
        {
            return null;
        }

        var pages = new Dictionary<int, OutlinePage>();
        foreach (var p in output.Pages)
        {
            if (p.Page is not { } index || index < 1 || index > count || pages.ContainsKey(index))
            {
                continue;
            }

            var kind = PageKinds.All.Contains(p.Kind) ? p.Kind! : PageKinds.Other;
            var start = p.ChapterStarts == true
                ? new ChapterStart(
                    p.ChapterNumber is >= 0 and <= 99 ? p.ChapterNumber : null,
                    JsonSchema.Clean(p.ChapterTitleKn, MaxTitle) ?? "",
                    JsonSchema.Clean(p.ChapterTitleEn, MaxTitle) ?? "")
                : null;
            pages[index] = new OutlinePage(
                firstPage + index - 1, p.PrintedPage is > 0 and <= MaxPrintedPage ? p.PrintedPage : null, kind, start);
        }

        return Enumerable.Range(1, count)
            .Select(i => pages.GetValueOrDefault(i) ?? new OutlinePage(firstPage + i - 1, null, PageKinds.Other, null))
            .ToList();
    }

    /// <summary>
    /// The chapters from every page of the book: a chapter runs from the page it starts on to the page before the next one
    /// starts. Front matter, the contents and answer pages belong to no chapter. Null when no chapter starts anywhere.
    /// </summary>
    public static BookOutline? Assemble(IReadOnlyList<OutlinePage> pages)
    {
        var ordered = pages.OrderBy(p => p.Number).ToList();
        // The contents page lists every chapter's title; only a chapter's own first page starts it.
        var starts = ordered.Where(p => p.ChapterStart is not null && p.Kind is not (PageKinds.Contents or PageKinds.Front)).ToList();
        if (starts.Count == 0)
        {
            return null;
        }

        var flags = new List<string>();
        var given = starts.Select(s => s.ChapterStart!.Number).ToList();
        var numbersOk = given.All(n => n is not null)
                        && given.Zip(given.Skip(1)).All(pair => pair.First < pair.Second);
        if (!numbersOk)
        {
            flags.Add("Chapter numbers were missing or out of order in the book as read, so they were numbered in order.");
        }

        var first = given[0] ?? 1;
        var chapters = new List<OutlineChapter>();
        for (var i = 0; i < starts.Count; i++)
        {
            var from = starts[i].Number;
            var to = i + 1 < starts.Count ? starts[i + 1].Number - 1 : ordered[^1].Number;
            var number = numbersOk ? given[i]!.Value : first + i;
            var start = starts[i].ChapterStart!;
            var nameEn = start.TitleEn.Length > 0 ? start.TitleEn : $"Chapter {number}";
            var nameKn = start.TitleKn.Length > 0 ? start.TitleKn : nameEn;
            if (start.TitleKn.Length == 0)
            {
                flags.Add($"Chapter {number} has no Kannada title in the book as read.");
            }

            var teaching = ordered.Where(p => p.Number >= from && p.Number <= to && PageKinds.Teaches(p.Kind)).Select(p => p.Number).ToList();
            if (teaching.Count > 0)
            {
                chapters.Add(new OutlineChapter(number, nameKn, nameEn, teaching));
            }
        }

        var printed = ordered.Where(p => p.PrintedPage is not null).ToDictionary(p => p.Number, p => p.PrintedPage!.Value);
        return chapters.Count == 0 ? null : new BookOutline(chapters, printed, flags);
    }

    /// <summary>What A10 gets with the pages: no names, only the book's description and where the window is.</summary>
    public static string UserContent(ImportBook book, int firstPage, int count) =>
        JsonSerializer.Serialize(new
        {
            grade = book.Grade,
            subject = book.Subject,
            medium = book.Medium,
            pages = count,
            firstPdfPage = firstPage,
        }, Json);

    private sealed record OutlineOutput(List<OutlinePageOutput>? Pages);

    private sealed record OutlinePageOutput(
        int? Page, int? PrintedPage, string? Kind, bool? ChapterStarts, int? ChapterNumber, string? ChapterTitleKn, string? ChapterTitleEn);
}
