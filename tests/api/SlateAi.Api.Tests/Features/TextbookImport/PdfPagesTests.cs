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
    public void A_book_splits_into_one_pdf_per_page_and_joins_back_in_order()
    {
        var pages = PdfPages.Split(Book(5), maxPages: 600);

        Assert.Equal(5, pages.Count);
        Assert.All(pages, p => Assert.Equal(1, PdfPages.Count(p)));
        var joined = PdfPages.Join([pages[1], pages[2], pages[3]]);
        Assert.Equal(["page 2", "page 3", "page 4"], Texts(joined));
    }

    [Fact]
    public void Not_a_pdf_too_many_pages_or_no_pages_are_refused()
    {
        Assert.Throws<PdfUnreadableException>(() => PdfPages.Split(Encoding.UTF8.GetBytes("hello"), maxPages: 600));
        var tooLong = Assert.Throws<PdfUnreadableException>(() => PdfPages.Split(Book(3), maxPages: 2));
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
