using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>What every generated PDF shares: A4 pages, the synthetic marking, fixed metadata and byte-stable output.</summary>
public static class SyntheticPdf
{
    internal static PdfDocument Create(DateOnly date)
    {
        EmbeddedFontResolver.Install();
        var doc = new PdfDocument();
        doc.Info.Title = VubStatementRenderer.SyntheticMarking;
        doc.Info.Subject = VubStatementRenderer.SyntheticMarking;
        doc.Info.Creator = "DocuSpend sample generator";
        doc.Info.CreationDate = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return doc;
    }

    /// <summary>The top of the footer marking, below everything the statement layout prints.</summary>
    public const double FooterY = 826.0;

    /// <summary>Adds an A4 page with the marking already in its left margin and footer.</summary>
    internal static (XGraphics Graphics, Canvas Canvas) AddPage(PdfDocument doc)
    {
        var page = doc.AddPage();
        page.Width = XUnit.FromPoint(VubStatementRenderer.PageWidth);
        page.Height = XUnit.FromPoint(VubStatementRenderer.PageHeight);
        var gfx = XGraphics.FromPdfPage(page);
        var canvas = new Canvas(gfx);
        canvas.Vertical(33.0, 450.0, VubStatementRenderer.SyntheticMarking, 6);
        canvas.Sans(45.4, FooterY, FictionalBank.Footer, 7);
        return (gfx, canvas);
    }

    internal static byte[] Save(PdfDocument doc, string seed)
    {
        using var stream = new MemoryStream();
        doc.Save(stream);
        return PdfNormalizer.Normalize(stream.ToArray(), seed);
    }
}
