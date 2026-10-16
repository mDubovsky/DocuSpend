using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DocuSpend.SampleGenerator.LeakChecking;

/// <summary>The searchable text of one sample file: PDF words or record JSON.</summary>
public sealed record SampleText(string Name, string Text);

public sealed record FileResult(string Name, int Strong, int Weak);

public sealed record LeakReport(IReadOnlyList<FileResult> Files)
{
    public bool HasStrongMatches => Files.Any(f => f.Strong > 0);

    /// <summary>Counts per file only. Never includes a matched value.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var f in Files)
            sb.AppendLine($"{f.Name}: {f.Strong} strong, {f.Weak} weak");
        sb.AppendLine(HasStrongMatches
            ? "FAIL: some samples contain values from the real statement (amounts, references, account numbers)."
            : "OK: no amounts, references or account numbers from the real statement.");
        if (Files.Any(f => f.Weak > 0))
            sb.AppendLine("Weak matches are words such as names or towns shared with the real statement; review them if the count is unexpected.");
        return sb.ToString();
    }
}

/// <summary>
/// Finds values from a real statement in the samples. Tokens are compared after normalization: amounts as numbers
/// (so "-1.234,56" in a PDF matches -1234.56 in JSON), account numbers without spaces, words case-insensitively.
/// Words that belong to the statement template (labels, bank boilerplate) are ignored.
/// </summary>
public static partial class LeakCheck
{
    public static LeakReport Run(IEnumerable<string> realWords, IEnumerable<string> templateWords, IEnumerable<SampleText> samples)
    {
        var template = templateWords.Select(Normalize).ToHashSet();
        var realList = realWords.ToList();
        var real = realList.Select(Normalize).Where(t => t.Length >= 3 && !template.Contains(t)).ToHashSet();
        real.UnionWith(Ibans(string.Join(' ', realList)));

        var results = samples.Select(sample =>
        {
            var tokens = Tokens(sample.Text).Select(Normalize).ToHashSet();
            tokens.UnionWith(Ibans(sample.Text));
            var matches = tokens.Where(real.Contains).ToList();
            return new FileResult(sample.Name, matches.Count(IsStrong), matches.Count(t => !IsStrong(t)));
        }).ToList();

        return new LeakReport(results);
    }

    /// <summary>Amounts, account numbers, and numbers of five or more characters (references, symbols, dates).</summary>
    private static bool IsStrong(string token) =>
        token.StartsWith("AMOUNT:", StringComparison.Ordinal)
        || token.StartsWith("IBAN:", StringComparison.Ordinal)
        || (token.Length >= 5 && token.Any(char.IsAsciiDigit));

    private static IEnumerable<string> Tokens(string text) =>
        text.Split((char[])[' ', '\t', '\r', '\n', '"', '(', ')', '[', ']', '{', '}', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.TrimEnd(':', ','));

    private static string Normalize(string token)
    {
        token = token.Trim().TrimEnd(':', ',');
        if (VubAmount().IsMatch(token))
            return Amount(decimal.Parse(token.Replace(".", "").Replace(',', '.'), CultureInfo.InvariantCulture));
        if (InvariantAmount().IsMatch(token))
            return Amount(decimal.Parse(token, CultureInfo.InvariantCulture));
        return token.ToUpperInvariant();
    }

    private static string Amount(decimal value) => "AMOUNT:" + value.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>IBANs in the text, printed with spaces or not, as "IBAN:" + the compact form.</summary>
    private static IEnumerable<string> Ibans(string text) =>
        Iban().Matches(text).Select(m => "IBAN:" + m.Value.Replace(" ", "").ToUpperInvariant());

    [GeneratedRegex(@"^-?\d{1,3}(\.\d{3})*,\d{2}$")]
    private static partial Regex VubAmount();

    [GeneratedRegex(@"^-?\d+\.\d{2}$")]
    private static partial Regex InvariantAmount();

    [GeneratedRegex(@"\b[A-Z]{2}\d{2}(?: ?\d{4}){4,7}\b")]
    private static partial Regex Iban();
}
