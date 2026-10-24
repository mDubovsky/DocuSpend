namespace DocuSpend.Privacy.Tests;

/// <summary>No real statements, or look-alikes, are committed (spec: privacy-no-real-data).</summary>
[Trait("Check", "privacy-no-real-data")]
public class RealDataTests
{
    [Theory]
    [InlineData("statement.pdf")]
    [InlineData("docs/VYPIS.PDF")]
    [InlineData("samples/march.xlsx")]
    [InlineData("tools/export.csv")]
    [InlineData("work/004-privacy-guard-rails/example.ofx")]
    [InlineData("data/2026.qif")]
    [InlineData("data/2026.mt940")]
    [InlineData("data/2026.sta")]
    [InlineData("data/2026.camt")]
    [InlineData("data/2026.ods")]
    [InlineData("data/2026.xls")]
    [InlineData("samples/publicity/statement.pdf")]
    [InlineData("Samples/Public/statement.pdf")]
    public void A_statement_like_file_outside_samples_public_fails(string path)
    {
        var problem = Assert.Single(RealDataPolicy.Violations(["README.md", path]));

        Assert.StartsWith($"privacy-no-real-data: {path} looks like a statement and is outside samples/public.", problem);
        Assert.Contains("samples/private", problem);
        Assert.Contains("DocuSpend.SampleGenerator -- generate", problem);
    }

    [Theory]
    [InlineData("samples/private/2026-03.pdf")]
    [InlineData("samples/private/notes.txt")]
    [InlineData("samples/private/sub/anything")]
    [InlineData("Samples/Private/2026-03.pdf")]
    public void Anything_tracked_under_samples_private_fails(string path)
    {
        var problem = Assert.Single(RealDataPolicy.Violations([path]));

        Assert.StartsWith($"privacy-no-real-data: {path} is tracked, but nothing under samples/private may be.", problem);
    }

    [Theory]
    [InlineData("samples/public/01-typical-month.pdf")]
    [InlineData("samples/public/01-typical-month.expected.json")]
    [InlineData("src/DocuSpend.Statements/Reading/VubStatementReader.cs")]
    [InlineData("tools/DocuSpend.SampleGenerator/Fonts/LiberationSans-Regular.ttf")]
    [InlineData("docs/statement-layout.md")]
    [InlineData("samples/README.md")]
    public void Ordinary_files_and_the_public_samples_pass(string path)
    {
        Assert.Empty(RealDataPolicy.Violations([path]));
    }

    [Fact]
    public void Every_problem_is_reported()
    {
        Assert.Equal(3, RealDataPolicy.Violations(["a.pdf", "b.csv", "samples/private/c.pdf", "d.md"]).Count);
    }

    [Fact]
    public void The_repository_tracks_no_real_data()
    {
        var tracked = TrackedFiles.Read(Repo.Root);
        var problems = RealDataPolicy.Violations(tracked);

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void The_tracked_files_are_read_from_the_index()
    {
        // Guards the reader: if it returned nothing, the test above would pass for the wrong reason.
        var tracked = TrackedFiles.Read(Repo.Root);

        Assert.Contains("AGENTS.md", tracked);
        Assert.Contains("samples/public/01-typical-month.pdf", tracked);
        Assert.Contains("src/DocuSpend.Statements/Reading/VubStatementReader.cs", tracked);
        Assert.DoesNotContain(tracked, p => p.Contains('\\'));
    }

    [Fact]
    public void Outside_a_git_repository_the_check_fails_rather_than_passing()
    {
        var dir = Directory.CreateTempSubdirectory("docuspend-no-git-");
        try
        {
            Assert.Throws<InvalidOperationException>(() => TrackedFiles.Read(dir.FullName));
        }
        finally
        {
            dir.Delete();
        }
    }
}
