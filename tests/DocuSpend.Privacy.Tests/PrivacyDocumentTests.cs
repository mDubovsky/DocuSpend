namespace DocuSpend.Privacy.Tests;

/// <summary>PRIVACY.md keeps the sections the spec requires, including what the checks don't cover.</summary>
public class PrivacyDocumentTests
{
    private static readonly string Text = File.ReadAllText(Repo.PathOf("PRIVACY.md"));

    [Theory]
    [InlineData("## What DocuSpend does with your statement")]
    [InlineData("## What it never does")]
    [InlineData("## How this is enforced")]
    [InlineData("## Check it yourself")]
    [InlineData("## What the checks don't cover")]
    [InlineData("## A possible optional online feature")]
    public void It_has_each_required_section(string heading)
    {
        Assert.Contains(heading, Text);
    }

    [Fact]
    public void It_gives_the_command_to_verify_a_download_against_this_repository()
    {
        Assert.Contains("gh attestation verify DocuSpend.StatementCheck-win-x64.zip --repo mDubovsky/DocuSpend", Text);
    }

    [Fact]
    public void It_links_to_where_the_checks_run_and_to_the_decision()
    {
        Assert.Contains("https://github.com/mDubovsky/DocuSpend/actions", Text);
        Assert.Contains("docs/decisions/0006-privacy-approach-and-network-boundary.md", Text);
    }
}
