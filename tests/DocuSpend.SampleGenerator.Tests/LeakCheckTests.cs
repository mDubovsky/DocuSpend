using DocuSpend.SampleGenerator.LeakChecking;

namespace DocuSpend.SampleGenerator.Tests;

/// <summary>The leak check's matching logic, on invented inputs only.</summary>
public class LeakCheckTests
{
    private static readonly string[] Template = ["Názov:", "PLATBA", "KARTOU", "EUR"];

    private static LeakReport Check(string[] real, params (string Name, string Text)[] samples) =>
        LeakCheck.Run(real, Template, samples.Select(s => new SampleText(s.Name, s.Text)));

    [Fact]
    public void Clean_samples_have_no_matches()
    {
        var report = Check(["Názov:", "Alžbeta", "Pokusná", "-42,17", "0101229000123"], ("a.pdf", "PLATBA KARTOU Jana Vzorová -12,50"));

        Assert.False(report.HasStrongMatches);
        Assert.Equal(0, report.Files.Single().Strong);
        Assert.Equal(0, report.Files.Single().Weak);
    }

    [Fact]
    public void Template_words_are_ignored()
    {
        var report = Check(["PLATBA", "KARTOU", "EUR"], ("a.pdf", "PLATBA KARTOU EUR"));

        Assert.Equal(0, report.Files.Single().Weak);
    }

    [Fact]
    public void An_amount_matches_across_formats()
    {
        var report = Check(["-1.234,56"], ("a.expected.json", "\"amount\": -1234.56"));

        Assert.True(report.HasStrongMatches);
        Assert.Equal(1, report.Files.Single().Strong);
    }

    [Fact]
    public void A_reference_or_symbol_is_a_strong_match()
    {
        var report = Check(["0101229000123", "7501126"], ("a.pdf", "x 0101229000123 y 7501126"));

        Assert.Equal(2, report.Files.Single().Strong);
    }

    [Fact]
    public void An_iban_matches_with_or_without_spaces()
    {
        var report = Check(["SK31", "1200", "0000", "1987", "4263", "7541"], ("a.expected.json", "\"iban\": \"SK3112000000198742637541\""));

        Assert.True(report.HasStrongMatches);
    }

    [Fact]
    public void Short_number_fragments_are_not_strong()
    {
        var report = Check(["0200", "0000"], ("a.pdf", "SK12 0200 0000 1111 2222 3333"));

        Assert.False(report.HasStrongMatches);
    }

    [Fact]
    public void Names_are_weak_matches_and_case_insensitive()
    {
        var report = Check(["Alžbeta", "POKUSNÁ"], ("a.pdf", "ALŽBETA Pokusná"));

        Assert.False(report.HasStrongMatches);
        Assert.Equal(2, report.Files.Single().Weak);
    }

    [Fact]
    public void Report_never_contains_the_values()
    {
        var report = Check(["-1.234,56", "Alžbeta"], ("a.pdf", "-1.234,56 Alžbeta"));

        var text = report.ToString();
        Assert.DoesNotContain("1.234,56", text);
        Assert.DoesNotContain("1234", text);
        Assert.DoesNotContain("Alžbeta", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("a.pdf", text);
    }
}
