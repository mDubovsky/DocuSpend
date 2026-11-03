using DocuSpend.Statements.Reading;

namespace DocuSpend.StatementCheck.Tests;

/// <summary>What the check prints for a statement it read (work/041-unknown-transaction-kinds/spec.md).</summary>
public sealed class CheckLineTests
{
    private static readonly string Samples = FindSamples();

    private static string LineFor(string sample)
    {
        var statement = new VubStatementReader().Read(File.ReadAllBytes(Path.Join(Samples, sample + ".pdf"))).Statement!;
        return CheckLine.Ok(sample + ".pdf", 2, statement);
    }

    [Fact]
    public void The_line_counts_the_transactions_of_unknown_type() =>
        Assert.Equal("10-new-types-month.pdf: OK (2 pages, 12 transactions, 2 of unknown type)", LineFor("10-new-types-month"));

    [Fact]
    public void A_statement_without_unknown_types_counts_none() =>
        Assert.EndsWith(", 0 of unknown type)", LineFor("01-typical-month"));

    [Fact]
    public void The_line_quotes_nothing_from_the_statement()
    {
        var line = LineFor("10-new-types-month");

        Assert.DoesNotContain("HODINKAMI", line);
        Assert.DoesNotContain("ZÁLOHY", line);
        Assert.DoesNotContain("SKÚŠKOVÁ", line);
    }

    private static string FindSamples()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Join(dir.FullName, "DocuSpend.slnx")))
                return Path.Join(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
