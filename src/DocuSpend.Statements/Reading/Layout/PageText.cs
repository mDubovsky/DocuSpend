using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocuSpend.Statements.Reading.Layout;

/// <summary>
/// A word with its position, in points from the page's top-left corner. <see cref="Top"/> is the top of its glyphs, which
/// varies with accents and size; <see cref="Baseline"/> doesn't, so lines are grouped by baseline.
/// </summary>
internal sealed record Word(int Page, double X, double Right, double Top, double Baseline, string Text);

/// <summary>Words sharing a visual line, left to right.</summary>
internal sealed record Line(int Page, double Top, IReadOnlyList<Word> Words)
{
    public double X => Words[0].X;

    public string Text => string.Join(' ', Words.Select(w => w.Text));

    /// <summary>The word starting within <paramref name="tolerance"/> of x, if any.</summary>
    public Word? StartingAt(double x, double tolerance = VubLayout.ColumnTolerance) =>
        Words.FirstOrDefault(w => Math.Abs(w.X - x) <= tolerance);

    /// <summary>The word ending within <paramref name="tolerance"/> of x, if any.</summary>
    public Word? EndingAt(double right, double tolerance = VubLayout.ColumnTolerance) =>
        Words.FirstOrDefault(w => Math.Abs(w.Right - right) <= tolerance);

    /// <summary>Words starting at or after <paramref name="from"/> and before <paramref name="to"/>, joined by single spaces.</summary>
    public string? TextBetween(double from, double to)
    {
        var words = Words.Where(w => w.X >= from - VubLayout.ColumnTolerance && w.X < to - VubLayout.ColumnTolerance).ToList();
        return words.Count == 0 ? null : string.Join(' ', words.Select(w => w.Text));
    }

    /// <summary>Whether the words <paramref name="sequence"/> appear on this line one after another.</summary>
    public bool Contains(params string[] sequence)
    {
        for (var i = 0; i + sequence.Length <= Words.Count; i++)
        {
            if (sequence.Select((s, k) => Words[i + k].Text == s).All(match => match))
                return true;
        }
        return false;
    }
}

internal static class PageText
{
    /// <summary>
    /// The horizontal words of every page. Vertical text, such as margin codes and markings, is left out.
    /// Only positions and text are used, never fonts, because real statements and samples use different fonts.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Word>> Words(PdfDocument doc) =>
        doc.GetPages()
            .Select(page => (IReadOnlyList<Word>)page.GetWords()
                .Where(w => w.TextOrientation == TextOrientation.Horizontal)
                .Select(w => new Word(page.Number, w.BoundingBox.Left, w.BoundingBox.Right, page.Height - w.BoundingBox.Top,
                    page.Height - w.Letters[0].StartBaseLine.Y, w.Text))
                .ToList())
            .ToList();

    /// <summary>Groups a page's words into lines, top to bottom.</summary>
    public static IReadOnlyList<Line> Lines(IEnumerable<Word> words)
    {
        var lines = new List<Line>();
        var current = new List<Word>();
        double baseline = 0;
        foreach (var word in words.OrderBy(w => w.Baseline))
        {
            if (current.Count > 0 && word.Baseline - baseline > VubLayout.LineTolerance)
            {
                lines.Add(MakeLine(current));
                current = [];
            }
            if (current.Count == 0)
                baseline = word.Baseline;
            current.Add(word);
        }
        if (current.Count > 0)
            lines.Add(MakeLine(current));
        return lines;
    }

    private static Line MakeLine(List<Word> words)
    {
        var ordered = words.OrderBy(w => w.X).ToList();
        return new Line(ordered[0].Page, words.Min(w => w.Top), ordered);
    }
}
