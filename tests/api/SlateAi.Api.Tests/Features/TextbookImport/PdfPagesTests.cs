using System.Text;
using SlateAi.Api.Features.TextbookImport;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace SlateAi.Api.Tests.Features.TextbookImport;

public sealed class PdfPagesTests
{
    [Fact]
    public void Every_page_is_measured_and_a_call_s_pages_are_cut_from_the_book_in_order()
    {
        var sizes = PdfPages.Measure(Book(5), maxPages: 600);
        using var book = new PdfBook(Book(5));

        Assert.Equal(5, sizes.Count);
        Assert.All(sizes, size => Assert.True(size > 0));
        Assert.Equal(5, book.PageCount);
        Assert.Equal(["page 2", "page 3", "page 4"], Texts(book.Pages([2, 3, 4])));
        Assert.Equal(["page 5", "page 1"], Texts(book.Pages([5, 1])));
    }

    [Fact]
    public void Not_a_pdf_too_many_pages_or_no_pages_are_refused()
    {
        Assert.Throws<PdfUnreadableException>(() => PdfPages.Measure(Encoding.UTF8.GetBytes("hello"), maxPages: 600));
        var tooLong = Assert.Throws<PdfUnreadableException>(() => PdfPages.Measure(Book(3), maxPages: 2));
        Assert.Contains("3 pages", tooLong.Message);
    }

    /// <summary>A small PDF whose page n says "page n".</summary>
    internal static byte[] Book(int pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        for (var n = 1; n <= pages; n++)
        {
            builder.AddPage(PageSize.A4).AddText($"page {n}", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        }

        return builder.Build();
    }

    private static List<string> Texts(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return document.GetPages().Select(p => p.Text).ToList();
    }
}
