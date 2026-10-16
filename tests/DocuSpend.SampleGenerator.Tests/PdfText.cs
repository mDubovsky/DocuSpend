using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>Reads a PDF the way a future parser will: words with positions, measured from the top of the page.</summary>
public sealed record PdfWord(int Page, double X, double Right, double Top, string Text, double Size, bool Bold);

public static class PdfText
{
    public static IReadOnlyList<PdfWord> Words(byte[] pdf)
    {
        using var doc = PdfDocument.Open(pdf);
        var words = new List<PdfWord>();
        foreach (var page in doc.GetPages())
        {
            foreach (var w in page.GetWords().Where(w => w.TextOrientation == TextOrientation.Horizontal))
            {
                var first = w.Letters[0];
                words.Add(new PdfWord(page.Number, w.BoundingBox.Left, w.BoundingBox.Right,
                    page.Height - w.BoundingBox.Top, w.Text, first.PointSize,
                    first.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true));
            }
        }
        return words;
    }

    /// <summary>Text of the vertical margin of each page, letters joined top to bottom.</summary>
    public static IReadOnlyList<string> MarginTexts(byte[] pdf)
    {
        using var doc = PdfDocument.Open(pdf);
        return doc.GetPages()
            .Select(p => string.Concat(p.Letters
                .Where(l => l.StartBaseLine.X < 42 && l.TextOrientation != TextOrientation.Horizontal)
                .OrderBy(l => p.Height - l.StartBaseLine.Y)
                .Select(l => l.Value)))
            .ToList();
    }

    public static int PageCount(byte[] pdf)
    {
        using var doc = PdfDocument.Open(pdf);
        return doc.NumberOfPages;
    }

    public static IReadOnlyDictionary<string, string> Info(byte[] pdf)
    {
        using var doc = PdfDocument.Open(pdf);
        var info = doc.Information;
        var result = new Dictionary<string, string>();
        if (info.Title is not null) result["Title"] = info.Title;
        if (info.Subject is not null) result["Subject"] = info.Subject;
        return result;
    }

    /// <summary>Words on one visual line: same page, tops within a tolerance.</summary>
    public static IReadOnlyList<PdfWord> Line(this IEnumerable<PdfWord> words, PdfWord anchor, double tolerance = 3) =>
        words.Where(w => w.Page == anchor.Page && Math.Abs(w.Top - anchor.Top) <= tolerance).OrderBy(w => w.X).ToList();
}
