using System.Globalization;

namespace DocuSpend.Presentation;

/// <summary>
/// The one place where amounts, dates and typed numbers are formatted and read. The interface is always Slovak
/// (decision 0003), so everything uses the sk-SK culture explicitly, never the thread's, and English Windows
/// changes nothing.
/// </summary>
public static class SlovakFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("sk-SK");

    /// <summary>"1 349,87 €". Statements are in euros.</summary>
    public static string Amount(decimal amount) => amount.ToString("C2", Culture);

    /// <summary>
    /// An amount as the user would type it into a field: a decimal comma, no thousands spaces and no "€", with the
    /// amount's own decimal places ("12,50"), so <see cref="TryParseAmount"/> reads it back unchanged.
    /// </summary>
    public static string AmountInput(decimal amount) => amount.ToString(Culture);

    /// <summary>"24. 10. 2026".</summary>
    public static string Date(DateOnly date) => date.ToString("d", Culture);

    /// <summary>"1. 3. 2026 – 31. 3. 2026".</summary>
    public static string Period(DateOnly start, DateOnly end) => $"{Date(start)} – {Date(end)}";

    /// <summary>A share from 0 to 1 as a percentage with one decimal: "47,4 %".</summary>
    public static string Share(decimal share) => share.ToString("P1", Culture);

    /// <summary>
    /// Reads an amount typed the Slovak way: a decimal comma, optional spaces between thousands, an optional "€".
    /// A decimal point is refused rather than guessed at, since "1.500" could mean either.
    /// </summary>
    public static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var group = Culture.NumberFormat.NumberGroupSeparator;
        var normalized = text.Trim().TrimEnd('€').Trim()
            .Replace(" ", group, StringComparison.Ordinal)
            .Replace(" ", group, StringComparison.Ordinal)
            .Replace(" ", group, StringComparison.Ordinal);
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            Culture, out amount);
    }
}
