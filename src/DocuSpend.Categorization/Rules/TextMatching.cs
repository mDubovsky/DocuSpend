using System.Globalization;
using System.Text;

namespace DocuSpend.Categorization.Rules;

/// <summary>
/// How rule conditions compare text. Case and diacritics never matter, so "lekaren" matches "LEKÁREŇ", and runs of
/// whitespace count as one space. Known limit: letters that don't decompose into a base letter and a mark
/// (for example Polish "ł") are not folded; they don't occur in Slovak or Czech.
/// </summary>
internal static class TextMatching
{
    /// <summary>Lower case, diacritics removed, every run of whitespace replaced by one space.</summary>
    public static string Fold(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var folded = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                    folded.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                folded.Append(char.ToLowerInvariant(c));
                lastWasSpace = false;
            }
        }
        return folded.ToString();
    }

    /// <summary>
    /// Whether the text contains the pattern. The text is padded with a space at each end first, so a pattern with a
    /// leading or trailing space ("MOL ", "OBI ") marks a word edge, also at the very start or end of the text.
    /// </summary>
    public static bool Contains(string? text, string pattern)
    {
        var folded = Fold(text).Trim();
        if (folded.Length == 0)
            return false;
        return $" {folded} ".Contains(Fold(pattern), StringComparison.Ordinal);
    }

    /// <summary>Whether the whole text is the value, apart from surrounding spaces.</summary>
    public static bool Equal(string? text, string value)
    {
        var folded = Fold(text).Trim();
        return folded.Length > 0 && folded == Fold(value).Trim();
    }

    /// <summary>Account numbers compare without spaces and ignoring case.</summary>
    public static bool SameAccount(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
        && string.Equals(Compact(a), Compact(b), StringComparison.Ordinal);

    public static bool IsBlank(string? text) => string.IsNullOrWhiteSpace(text);

    private static string Compact(string value) =>
        new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
}
