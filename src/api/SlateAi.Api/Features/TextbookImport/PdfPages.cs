using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>The PDF can't be opened or a page can't be copied: refused at upload, nothing is stored.</summary>
public sealed class PdfUnreadableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Cuts a few pages out of a textbook (T-146), so Claude reads a few pages at a time and a 200-page book never goes in
/// one request (32 MB limit). No text is extracted here: Claude reads the pages. The book is stored once and pages are
/// cut only when a call needs them: a page cut on its own carries the book's shared fonts and pictures, so storing every
/// page would take about five times the book's size.
/// </summary>
public static class PdfPages
{
    /// <summary>
    /// Every page's size when cut on its own, in order: proves each page can be copied and lets the import plan calls by
    /// bytes. The cut pages are not kept.
    /// </summary>
    /// <exception cref="PdfUnreadableException">Not a PDF, encrypted, too many pages, or a page that can't be copied.</exception>
    public static IReadOnlyList<int> Measure(byte[] pdf, int maxPages)
    {
        try
        {
            using var document = PdfDocument.Open(pdf);
            if (document.NumberOfPages == 0)
            {
                throw new PdfUnreadableException("The PDF has no pages.");
            }

            if (document.NumberOfPages > maxPages)
            {
                throw new PdfUnreadableException($"The PDF has {document.NumberOfPages} pages; the most is {maxPages}.");
            }

            var sizes = new List<int>(document.NumberOfPages);
            for (var number = 1; number <= document.NumberOfPages; number++)
            {
                var builder = new PdfDocumentBuilder();
                builder.AddPage(document, number);
                sizes.Add(builder.Build().Length);
            }

            return sizes;
        }
        catch (PdfUnreadableException)
        {
            throw;
        }
        catch (Exception e)
        {
            // PdfPig throws many kinds for a damaged, encrypted or non-PDF file; the admin only needs to know it can't be read.
            throw new PdfUnreadableException("The PDF can't be read.", e);
        }
    }

    /// <summary>The number of pages in a PDF.</summary>
    public static int Count(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return document.NumberOfPages;
    }
}

/// <summary>A book opened once for an import's run; each call cuts its pages from it.</summary>
public sealed class PdfBook(byte[] pdf) : IDisposable
{
    private readonly PdfDocument _document = PdfDocument.Open(pdf);

    public int PageCount => _document.NumberOfPages;

    /// <summary>One PDF of these pages (1-based PDF page numbers), in this order.</summary>
    public byte[] Pages(IReadOnlyList<int> numbers)
    {
        var builder = new PdfDocumentBuilder();
        foreach (var number in numbers)
        {
            builder.AddPage(_document, number);
        }

        return builder.Build();
    }

    public void Dispose() => _document.Dispose();
}
