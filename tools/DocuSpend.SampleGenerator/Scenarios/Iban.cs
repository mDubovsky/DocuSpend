using System.Numerics;

namespace DocuSpend.SampleGenerator.Scenarios;

/// <summary>
/// Structurally valid IBANs for invented accounts, printed in groups of four as on the statement. IBANs whose
/// length isn't a multiple of four end with a shorter group (a German one ends with two digits).
/// </summary>
public static class Iban
{
    /// <summary>A Slovak IBAN from a 4-digit bank code, 6-digit prefix and 10-digit account number.</summary>
    public static string Slovak(string bankCode, string prefix, string account)
    {
        var bban = bankCode + prefix + account;
        if (bban.Length != 20 || !bban.All(char.IsAsciiDigit))
            throw new ArgumentException("Slovak BBAN must be 20 digits.");
        return Create("SK", bban);
    }

    /// <summary>An IBAN for any country from its two-letter code and national account number (BBAN).</summary>
    public static string Create(string country, string bban)
    {
        if (country.Length != 2 || !country.All(char.IsAsciiLetterUpper) || bban.Length == 0 || !bban.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException("Expected a two-letter country code and an alphanumeric BBAN.");
        var check = 98 - Mod97(Numeric(bban + country + "00"));
        return Group($"{country}{check:00}{bban}");
    }

    public static bool IsValid(string iban)
    {
        var compact = iban.Replace(" ", "");
        return compact.Length > 4 && Mod97(Numeric(compact[4..] + compact[..4])) == 1;
    }

    private static string Group(string compact) =>
        string.Join(' ', compact.Chunk(4).Select(c => new string(c)));

    private static string Numeric(string s) =>
        string.Concat(s.Select(c => char.IsAsciiLetter(c) ? (char.ToUpperInvariant(c) - 'A' + 10).ToString() : c.ToString()));

    private static int Mod97(string digits) => (int)(BigInteger.Parse(digits) % 97);
}
