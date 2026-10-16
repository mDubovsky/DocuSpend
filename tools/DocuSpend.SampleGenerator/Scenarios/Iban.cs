using System.Numerics;

namespace DocuSpend.SampleGenerator.Scenarios;

/// <summary>Structurally valid IBANs for invented accounts, printed in groups of four as on the statement.</summary>
public static class Iban
{
    /// <summary>A Slovak IBAN from a 4-digit bank code, 6-digit prefix and 10-digit account number.</summary>
    public static string Slovak(string bankCode, string prefix, string account)
    {
        var bban = bankCode + prefix + account;
        if (bban.Length != 20 || !bban.All(char.IsAsciiDigit))
            throw new ArgumentException("Slovak BBAN must be 20 digits.");
        var check = 98 - Mod97(bban + Numeric("SK") + "00");
        return Group($"SK{check:00}{bban}");
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
