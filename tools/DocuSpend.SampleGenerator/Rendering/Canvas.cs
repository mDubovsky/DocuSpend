using PdfSharp.Drawing;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>
/// Draws text the way the real statement does: a monospaced font on a grid where each character advances
/// half its point size (5 pt at 10 pt). Liberation Mono advances 0.6 em, so glyphs are condensed by 5/6.
/// Coordinates are from the top-left corner, with y at the top of the text, as in layout.md.
/// </summary>
internal sealed class Canvas(XGraphics g, Func<string, string?>? transform = null)
{
    private const double Condense = 5.0 / 6.0;

    // The measured "top" is the top of the glyphs, slightly below the top of PDFsharp's line box.
    private const double TopOffset = 0.12;

    private readonly Dictionary<(string, double, bool), XFont> _fonts = [];

    public static double Advance(double size) => size * 0.5;

    public void Mono(double x, double y, string text, double size = 10, bool bold = false) =>
        DrawMono(x, y, Transform(text), size, bold);

    public void MonoRight(double right, double y, string text, double size = 10, bool bold = false)
    {
        // Transform first, so a changed text stays right-aligned.
        var drawn = Transform(text);
        if (drawn is not null)
            DrawMono(right - drawn.Length * Advance(size), y, drawn, size, bold);
    }

    private void DrawMono(double x, double y, string? text, double size, bool bold)
    {
        if (string.IsNullOrEmpty(text))
            return;
        var state = g.Save();
        g.ScaleTransform(Condense, 1);
        g.DrawString(text, Font(EmbeddedFontResolver.Mono, size, bold), XBrushes.Black,
            x / Condense, y - TopOffset * size, XStringFormats.TopLeft);
        g.Restore(state);
    }

    private string? Transform(string text) => transform is null || string.IsNullOrEmpty(text) ? text : transform(text);

    /// <summary>Label in bold followed by its value in regular, one character apart, as in the details blocks.</summary>
    public void Labeled(double x, double y, string label, string value, double size = 10)
    {
        Mono(x, y, label, size, bold: true);
        Mono(x + (label.Length + 1) * Advance(size), y, value, size);
    }

    public void Sans(double x, double y, string text, double size)
    {
        var drawn = Transform(text);
        if (string.IsNullOrEmpty(drawn))
            return;
        g.DrawString(drawn, Font(EmbeddedFontResolver.Sans, size, false), XBrushes.Black,
            x, y - TopOffset * size, XStringFormats.TopLeft);
    }

    /// <summary>Text running top to bottom, like the real statement's margin code.</summary>
    public void Vertical(double x, double y, string text, double size)
    {
        var state = g.Save();
        g.TranslateTransform(x, y);
        g.RotateTransform(90);
        g.DrawString(text, Font(EmbeddedFontResolver.Mono, size, false), XBrushes.Black, 0, 0, XStringFormats.TopLeft);
        g.Restore(state);
    }

    public void Rule(double x1, double y, double x2) =>
        g.DrawLine(new XPen(XColors.Black, 0.5), x1, y, x2, y);

    private XFont Font(string family, double size, bool bold)
    {
        var key = (family, size, bold);
        if (!_fonts.TryGetValue(key, out var font))
        {
            font = new XFont(family, size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
            _fonts[key] = font;
        }
        return font;
    }
}
