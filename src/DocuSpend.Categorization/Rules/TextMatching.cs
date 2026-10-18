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
    /// Like <see cref="Fold"/>, and every character that is not a letter or a digit also counts as a space, so
    /// "Mol.Benzinka" becomes "mol benzinka" and "DR.MAX" becomes "dr max". Used for *contains*, where punctuation in
    /// bank text ("Dr.Max 382", "GOOGLE*YouTubePremium") must not stop a name from being found.
    /// </summary>
    public static string FoldWords(string? text)
    {
        var folded = Fold(text);
        if (folded.Length == 0)
            return "";

        var words = new StringBuilder(folded.Length);
        var lastWasSpace = false;
        foreach (var c in folded)
        {
            if (char.IsLetterOrDigit(c))
            {
                words.Append(c);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                words.Append(' ');
                lastWasSpace = true;
            }
        }
        return words.ToString();
    }

    /// <summary>
    /// Whether the text contains the pattern, with punctuation counted as spaces. The text is padded with a space at
    /// each end first, so a pattern with a leading or trailing space ("MOL ", " OBI ") marks a word edge, also at the
    /// very start or end of the text and next to punctuation. A pattern with no letters or digits matches nothing.
    /// </summary>
    public static bool Contains(string? text, string pattern)
    {
        var words = FoldWords(text).Trim();
        var wanted = FoldWords(pattern);
        if (words.Length == 0 || HasNoWords(pattern))
            return false;
        return $" {words} ".Contains(wanted, StringComparison.Ordinal);
    }

    /// <summary>True when nothing is left of the text once spaces and punctuation are ignored.</summary>
    public static bool HasNoWords(string? text) => FoldWords(text).Trim().Length == 0;

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
