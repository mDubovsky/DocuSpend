using System.Globalization;

namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>Number and date formats used on VÚB statements.</summary>
public static class VubFormat
{
    /// <summary>"1.234,56", with a leading "-" for negative amounts.</summary>
    public static string Amount(decimal value)
    {
        var invariant = Math.Abs(value).ToString("#,##0.00", CultureInfo.InvariantCulture);
        var swapped = invariant.Replace(',', ' ').Replace('.', ',').Replace(' ', '.');
        return value < 0 ? "-" + swapped : swapped;
    }

    public static string TableDate(DateOnly date) => date.ToString("dd'/'MM", CultureInfo.InvariantCulture);

    public static string FullDate(DateOnly date) => date.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture);
}
