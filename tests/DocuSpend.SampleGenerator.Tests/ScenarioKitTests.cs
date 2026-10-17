using DocuSpend.SampleGenerator.Scenarios;

namespace DocuSpend.SampleGenerator.Tests;

public class ScenarioKitTests
{
    [Theory]
    [InlineData("1100", "000000", "2612345678")]
    [InlineData("9900", "000000", "0012345678")]
    [InlineData("0900", "000019", "0000123456")]
    public void Slovak_ibans_have_valid_check_digits(string bank, string prefix, string account)
    {
        var iban = Iban.Slovak(bank, prefix, account);

        Assert.Matches(@"^SK\d{2}( \d{4}){5}$", iban);
        Assert.True(Iban.IsValid(iban), iban);
    }

    [Fact]
    public void Foreign_ibans_have_valid_check_digits_and_a_short_last_group()
    {
        var german = Iban.Create("DE", "123456781234567890");

        Assert.Matches(@"^DE\d{2}( \d{4}){4} \d{2}$", german);
        Assert.True(Iban.IsValid(german), german);
    }

    [Fact]
    public void Iban_validation_rejects_a_wrong_check_digit()
    {
        var iban = Iban.Slovak("1100", "000000", "2612345678");
        var broken = iban[..2] + (iban[2] == '9' ? '0' : (char)(iban[2] + 1)) + iban[3..];

        Assert.False(Iban.IsValid(broken));
    }

    [Fact]
    public void Seeded_random_is_the_same_every_time()
    {
        var a = new SeededRandom(42);
        var b = new SeededRandom(42);

        Assert.Equal(Enumerable.Range(0, 20).Select(_ => a.Next(1000)), Enumerable.Range(0, 20).Select(_ => b.Next(1000)));
    }

    [Fact]
    public void Seeded_random_sequence_is_fixed()
    {
        // Pinned so a change to the generator can't silently change sample 2.
        var r = new SeededRandom(2026);

        Assert.Equal([51, 1, 34, 86, 41], Enumerable.Range(0, 5).Select(_ => r.Next(100)));
    }
}
