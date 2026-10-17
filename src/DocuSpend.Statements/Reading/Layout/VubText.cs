using System.Globalization;
using System.Text.RegularExpressions;

namespace DocuSpend.Statements.Reading.Layout;

/// <summary>Number and date formats printed on VÚB statements.</summary>
internal static partial class VubText
{
    /// <summary>"1.234,56" or "-7,00".</summary>
    public static bool IsAmount(string text) => AmountFormat().IsMatch(text);

    public static decimal? Amount(string? text) =>
        text is not null && IsAmount(text)
            ? decimal.Parse(text.Replace(".", "").Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
            : null;

    /// <summary>"31.08.2026".</summary>
    public static DateOnly? FullDate(string? text) =>
        DateOnly.TryParseExact(text, "dd'.'MM'.'yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    /// <summary>"31/08", the table's date without a year.</summary>
    public static bool IsTableDate(string text) => TableDateFormat().IsMatch(text);

    /// <summary>
    /// A "dd/mm" date with the year that places it nearest the statement period, so a January statement can show
    /// a value date in December of the year before.
    /// </summary>
    public static DateOnly? TableDate(string? text, DateOnly periodStart, DateOnly periodEnd)
    {
        if (text is null || !IsTableDate(text))
            return null;
        var day = int.Parse(text[..2], CultureInfo.InvariantCulture);
        var month = int.Parse(text[3..], CultureInfo.InvariantCulture);
        var candidates = new[] { periodStart.Year - 1, periodStart.Year, periodEnd.Year, periodEnd.Year + 1 }
            .Distinct()
            .Where(year => month <= 12 && day <= DateTime.DaysInMonth(year, Math.Max(month, 1)) && month >= 1 && day >= 1)
            .Select(year => new DateOnly(year, month, day))
            .ToList();
        return candidates.Count == 0 ? null : candidates.MinBy(d => Distance(d, periodStart, periodEnd));
    }

    private static int Distance(DateOnly date, DateOnly start, DateOnly end) =>
        date < start ? start.DayNumber - date.DayNumber : date > end ? date.DayNumber - end.DayNumber : 0;

    [GeneratedRegex(@"^-?\d{1,3}(\.\d{3})*,\d{2}$")]
    private static partial Regex AmountFormat();

    [GeneratedRegex(@"^\d{2}/\d{2}$")]
    private static partial Regex TableDateFormat();
}
