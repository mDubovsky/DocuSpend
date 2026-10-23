namespace DocuSpend.Privacy.Tests;

/// <summary>
/// privacy-no-real-data: no real statement, or anything that looks like one, is committed. Statement-like files
/// (PDFs, spreadsheets, CSV and bank-export formats) may be tracked only under samples/public, where every file is
/// one the sample generator produces (checked byte for byte by CommittedSamplesTests); nothing under
/// samples/private may be tracked at all.
/// </summary>
public static class RealDataPolicy
{
    public const string PublicSamples = "samples/public/";
    public const string PrivateSamples = "samples/private/";

    /// <summary>Formats a statement, or an export of one, can come in.</summary>
    public static readonly IReadOnlySet<string> StatementLikeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".xls", ".xlsx", ".xlsm", ".xlsb", ".ods", ".csv", ".tsv", ".ofx", ".qfx", ".qif", ".mt940", ".sta", ".camt",
    };

    private const string Why =
        "Real financial data must never be committed (AGENTS.md rule 4). Keep real statements in samples/private, which "
        + "git ignores, and use the synthetic samples in samples/public for tests; regenerate those with "
        + "dotnet run --project tools/DocuSpend.SampleGenerator -- generate (see samples/README.md).";

    /// <summary>The problems with a set of tracked paths (repository-relative, forward slashes).</summary>
    public static IReadOnlyList<string> Violations(IEnumerable<string> trackedPaths)
    {
        var problems = new List<string>();
        foreach (var path in trackedPaths.Order(StringComparer.Ordinal))
        {
            if (path.StartsWith(PrivateSamples, StringComparison.OrdinalIgnoreCase))
                problems.Add($"privacy-no-real-data: {path} is tracked, but nothing under samples/private may be. {Why}");
            else if (IsStatementLike(path) && !path.StartsWith(PublicSamples, StringComparison.Ordinal))
                problems.Add($"privacy-no-real-data: {path} looks like a statement and is outside samples/public. {Why}");
        }
        return problems;
    }

    public static bool IsStatementLike(string path) => StatementLikeExtensions.Contains(Path.GetExtension(path));
}
