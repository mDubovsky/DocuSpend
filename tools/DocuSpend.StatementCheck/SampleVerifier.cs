using System.Text.Json;
using DocuSpend.Categorization;
using DocuSpend.Statements.Reading;

namespace DocuSpend.StatementCheck;

/// <summary>The outcome of verifying a directory of samples: one line per sample, and whether all agreed.</summary>
internal sealed record VerifyReport(IReadOnlyList<string> Lines, bool Success);

/// <summary>
/// The read-and-categorize flow on the synthetic samples, checked against each sample's own .expected.json:
/// a sample expected to parse must read, have the expected closing balance and number of transactions, and
/// categorize completely, with totals that add up; a sample expected to be rejected must be rejected. Used by
/// --verify, which CI runs with the network switched off (privacy-no-network-run). The samples are synthetic,
/// so the report may name them.
/// </summary>
internal static class SampleVerifier
{
    public static VerifyReport Verify(string dir)
    {
        if (!Directory.Exists(dir))
            return new VerifyReport([$"{dir}: no such directory."], false);

        var pdfs = Directory.EnumerateFiles(dir, "*.pdf").Select(Path.GetFileNameWithoutExtension).OfType<string>().ToHashSet();
        var records = Directory.EnumerateFiles(dir, "*.expected.json").Select(f => Path.GetFileName(f)[..^".expected.json".Length]).ToHashSet();
        if (pdfs.Count == 0)
            return new VerifyReport([$"{dir}: no samples found."], false);

        var reader = new VubStatementReader();
        var lines = new List<string>();
        var success = true;
        foreach (var name in pdfs.Union(records).Order(StringComparer.Ordinal))
        {
            var (ok, line) = (pdfs.Contains(name), records.Contains(name)) switch
            {
                (true, false) => (false, "no .expected.json for this PDF"),
                (false, true) => (false, ".expected.json without its PDF"),
                _ => VerifyOne(reader, Path.Combine(dir, name)),
            };
            success &= ok;
            lines.Add($"{name}: {(ok ? "OK" : "FAILED")}, {line}");
        }
        lines.Add(success ? $"All {lines.Count} samples agree with their records." : "Some samples do not agree with their records.");
        return new VerifyReport(lines, success);
    }

    private static (bool Ok, string Line) VerifyOne(VubStatementReader reader, string pathWithoutExtension)
    {
        Expected expected;
        try
        {
            expected = Expected.Parse(File.ReadAllText(pathWithoutExtension + ".expected.json"));
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return (false, $"unreadable .expected.json ({e.Message})");
        }

        var result = reader.Read(File.ReadAllBytes(pathWithoutExtension + ".pdf"));
        if (expected.Rejected)
        {
            return result.Rejection is { } rejection
                ? (true, $"rejected as expected ({rejection.Kind})")
                : (false, "expected to be rejected, but it was read");
        }
        if (result.Statement is not { } statement)
            return (false, $"expected to be read, but it was rejected ({result.Rejection!.Kind})");
        if (statement.Transactions.Count != expected.Transactions)
            return (false, $"{statement.Transactions.Count} transactions read, {expected.Transactions} expected");
        if (statement.ClosingBalance != expected.ClosingBalance)
            return (false, "the closing balance read differs from the expected one");

        var categorized = Categorizer.Categorize(statement, CategorizationConfig.Default);
        if (!categorized.IsComplete)
            return (false, "categorized, but the category totals don't add up to the balance change");
        return (true, $"read and categorized, {statement.Transactions.Count} transactions, totals add up");
    }

    /// <summary>The parts of a sample's .expected.json that --verify compares.</summary>
    private sealed record Expected(bool Rejected, int Transactions, decimal ClosingBalance)
    {
        public static Expected Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return root.GetProperty("expectation").GetString() switch
            {
                "reject" => new Expected(true, 0, 0),
                "parse" => new Expected(false,
                    root.TryGetProperty("transactions", out var t) ? t.GetArrayLength() : 0,
                    root.GetProperty("statement").GetProperty("closingBalance").GetDecimal()),
                var other => throw new FormatException($"unknown expectation '{other}'"),
            };
        }
    }
}
