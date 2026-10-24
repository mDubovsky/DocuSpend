using System.Globalization;

namespace DocuSpend.Presentation.Look;

/// <summary>The WCAG contrast ratio of two colours, from 1 (the same) to 21 (black on white).</summary>
public static class Contrast
{
    public static double Ratio(string a, string b)
    {
        var (x, y) = (Luminance(a), Luminance(b));
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    /// <summary>Relative luminance of a "#RRGGBB" colour.</summary>
    public static double Luminance(string hex)
    {
        double Channel(int index)
        {
            var c = int.Parse(hex.AsSpan(1 + 2 * index, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(0) + 0.7152 * Channel(1) + 0.0722 * Channel(2);
    }
}
