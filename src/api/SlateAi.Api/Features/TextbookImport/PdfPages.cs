using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>The PDF can't be opened or a page can't be copied: refused at upload, nothing is stored.</summary>
public sealed class PdfUnreadableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Cuts a textbook into single-page PDFs and joins runs of them again (T-146), so Claude reads a few pages at a time and
/// a 200-page book never goes in one request (32 MB limit). No text is extracted here: Claude reads the pages.
/// </summary>
public static class PdfPages
{
    /// <summary>One single-page PDF per page, in order.</summary>
    /// <exception cref="PdfUnreadableException">Not a PDF, encrypted, or a page that can't be copied.</exception>
    public static IReadOnlyList<byte[]> Split(byte[] pdf, int maxPages)
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

            var pages = new List<byte[]>(document.NumberOfPages);
            for (var number = 1; number <= document.NumberOfPages; number++)
            {
                var builder = new PdfDocumentBuilder();
                builder.AddPage(document, number);
                pages.Add(builder.Build());
            }

            return pages;
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

    /// <summary>One PDF of these pages, in this order.</summary>
    public static byte[] Join(IReadOnlyList<byte[]> pages) =>
        pages.Count == 1 ? pages[0] : PdfMerger.Merge(pages);

    /// <summary>The number of pages in a PDF this class made.</summary>
    public static int Count(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return document.NumberOfPages;
    }
}
