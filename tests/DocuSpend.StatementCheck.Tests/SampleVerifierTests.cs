namespace DocuSpend.StatementCheck.Tests;

/// <summary>
/// StatementCheck --verify passes only when every sample agrees with its record (plan: the network-off run). The
/// disagreeing cases are copies of the committed samples paired with the wrong record, or missing a half.
/// </summary>
[Trait("Check", "privacy-no-network-run")]
public sealed class SampleVerifierTests : IDisposable
{
    private static readonly string Samples = FindSamples();

    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("docuspend-verify-");

    public void Dispose() => _dir.Delete(recursive: true);

    [Fact]
    public void The_committed_samples_all_agree_with_their_records()
    {
        var report = SampleVerifier.Verify(Samples);

        Assert.True(report.Success, string.Join(Environment.NewLine, report.Lines));
        Assert.Equal(10, report.Lines.Count);
        Assert.All(report.Lines.Take(5), l => Assert.Contains(": OK, read and categorized", l));
        Assert.All(report.Lines.Skip(5).Take(4), l => Assert.Contains(": OK, rejected as expected", l));
        Assert.Equal("All 9 samples agree with their records.", report.Lines[^1]);
    }

    [Fact]
    public void A_sample_expected_to_parse_that_is_rejected_fails()
    {
        Pair("07-not-a-statement.pdf", "01-typical-month.expected.json");

        AssertFails("x: FAILED, expected to be read, but it was rejected (NotAVubStatement)");
    }

    [Fact]
    public void A_sample_expected_to_be_rejected_that_is_read_fails()
    {
        Pair("01-typical-month.pdf", "06-other-bank-statement.expected.json");

        AssertFails("x: FAILED, expected to be rejected, but it was read");
    }

    [Fact]
    public void A_different_number_of_transactions_fails()
    {
        Pair("05-single-transaction-month.pdf", "01-typical-month.expected.json");

        Assert.Contains("transactions read", Assert.Single(SampleVerifier.Verify(_dir.FullName).Lines, l => l.StartsWith("x:")));
        Assert.False(SampleVerifier.Verify(_dir.FullName).Success);
    }

    [Fact]
    public void A_different_closing_balance_fails()
    {
        Copy("01-typical-month.pdf", "x.pdf");
        var record = File.ReadAllText(Path.Combine(Samples, "01-typical-month.expected.json"))
            .Replace("\"closingBalance\": 2041.19", "\"closingBalance\": 2041.20");
        File.WriteAllText(Path.Combine(_dir.FullName, "x.expected.json"), record);

        AssertFails("x: FAILED, the closing balance read differs from the expected one");
    }

    [Fact]
    public void A_pdf_without_a_record_fails()
    {
        Copy("01-typical-month.pdf", "x.pdf");

        AssertFails("x: FAILED, no .expected.json for this PDF");
    }

    [Fact]
    public void A_record_without_a_pdf_fails()
    {
        Copy("01-typical-month.pdf", "a.pdf");
        Copy("01-typical-month.expected.json", "a.expected.json");
        Copy("01-typical-month.expected.json", "x.expected.json");

        AssertFails("x: FAILED, .expected.json without its PDF");
    }

    [Theory]
    [InlineData("""{ "sample": "x", "expectation": "maybe" }""")]
    [InlineData("""{ "sample": "x" }""")]
    [InlineData("""{ "sample": "x", "expectation": "parse" }""")]
    [InlineData("not json")]
    public void An_unreadable_record_fails(string json)
    {
        Copy("01-typical-month.pdf", "x.pdf");
        File.WriteAllText(Path.Combine(_dir.FullName, "x.expected.json"), json);

        Assert.StartsWith("x: FAILED, unreadable .expected.json", Assert.Single(SampleVerifier.Verify(_dir.FullName).Lines, l => l.StartsWith("x:")));
    }

    [Fact]
    public void An_empty_or_missing_directory_fails_rather_than_passing()
    {
        Assert.False(SampleVerifier.Verify(_dir.FullName).Success);
        Assert.False(SampleVerifier.Verify(Path.Combine(_dir.FullName, "missing")).Success);
    }

    private void Pair(string pdf, string record)
    {
        Copy(pdf, "x.pdf");
        Copy(record, "x.expected.json");
    }

    private void Copy(string sample, string copyName) =>
        File.Copy(Path.Combine(Samples, sample), Path.Combine(_dir.FullName, copyName));

    private void AssertFails(string expectedLine)
    {
        var report = SampleVerifier.Verify(_dir.FullName);

        Assert.False(report.Success);
        Assert.Contains(expectedLine, report.Lines);
        Assert.Equal("Some samples do not agree with their records.", report.Lines[^1]);
    }

    private static string FindSamples()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return Path.Combine(dir.FullName, "samples", "public");
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}
