using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;
using static DocuSpend.Categorization.Tests.TestTransactions;

namespace DocuSpend.Categorization.Tests;

/// <summary>How a transaction is recognised again (work/022-categorize-from-list/spec.md: what is kept).</summary>
public class FingerprintTests
{
    private static readonly FingerprintKey Key = FingerprintKey.Create();

    private static Fingerprint One(Transaction transaction, FingerprintKey? key = null) =>
        Fingerprints.Of(TestStatements.Of(transaction), key ?? Key)[0];

    [Fact]
    public void The_same_transaction_gives_the_same_fingerprint_in_another_statement()
    {
        var lidl = Card("LIDL BRATISLAVA 1234", -23.45m);
        var other = TestStatements.Of(Card("TESCO"), lidl with { }, Transfer(-5m, name: "Ján"));

        Assert.Equal(One(lidl), Fingerprints.Of(other, Key)[1]);
    }

    [Fact]
    public void A_fingerprint_is_64_lower_case_hex_characters()
    {
        var value = One(Card("LIDL")).Value;

        Assert.Equal(64, value.Length);
        Assert.True(Fingerprint.IsValid(value));
    }

    public static TheoryData<string> Changes => new() { "date", "amount", "description", "name", "account" };

    [Theory]
    [MemberData(nameof(Changes))]
    public void Any_detail_changed_gives_another_fingerprint(string change)
    {
        var original = Transfer(-50m, description: "Nájom", name: "Ján Vzorový", account: "SK00 1111");
        var changed = change switch
        {
            "date" => original with { BookingDate = original.BookingDate.AddDays(1) },
            "amount" => original with { Amount = -50.01m },
            "description" => original with { Description = "Nájom marec" },
            "name" => original with { CounterpartyName = "Jan Vzorovy" },
            _ => original with { CounterpartyAccount = "SK00 1112" },
        };

        Assert.NotEqual(One(original), One(changed));
    }

    [Fact]
    public void Details_that_are_not_part_of_it_do_not_change_it()
    {
        var original = Transfer(-50m, description: "Nájom", name: "Ján", variableSymbol: "1");

        Assert.Equal(One(original), One(original with { VariableSymbol = "2", ValueDate = original.ValueDate.AddDays(-2), PaymentPurpose = "x" }));
    }

    [Fact]
    public void A_missing_detail_differs_from_an_empty_one() =>
        Assert.NotEqual(One(Transfer(description: null)), One(Transfer(description: "")));

    [Fact]
    public void Details_never_run_together()
    {
        // "AB" + "C" against "A" + "BC" would be the same text without the lengths.
        Assert.NotEqual(
            One(Transfer(description: "AB", name: "C")),
            One(Transfer(description: "A", name: "BC")));
    }

    [Fact]
    public void Identical_transactions_are_told_apart_by_their_place_among_them()
    {
        var coffee = Card("KAVIAREŇ", -2.40m);
        var statement = TestStatements.Of(coffee, Card("LIDL"), coffee with { }, coffee with { });

        var prints = Fingerprints.Of(statement, Key);

        Assert.Equal(4, prints.Distinct().Count());
        Assert.Equal(One(coffee), prints[0]);
    }

    [Fact]
    public void The_place_among_identical_ones_ignores_other_transactions_in_between()
    {
        var coffee = Card("KAVIAREŇ", -2.40m);

        var a = Fingerprints.Of(TestStatements.Of(coffee, coffee with { }), Key);
        var b = Fingerprints.Of(TestStatements.Of(Card("LIDL"), coffee, Transfer(), coffee with { }), Key);

        Assert.Equal(a, [b[1], b[3]]);
    }

    [Fact]
    public void Another_key_gives_another_fingerprint() =>
        Assert.NotEqual(One(Card("LIDL")), One(Card("LIDL"), FingerprintKey.Create()));

    [Fact]
    public void A_key_survives_its_text()
    {
        var text = Key.ToText();

        Assert.Equal(One(Card("LIDL")), One(Card("LIDL"), FingerprintKey.FromText(text)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("AAAA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void A_key_text_that_is_not_32_bytes_is_no_key(string? text) => Assert.Null(FingerprintKey.FromText(text));

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("3BC2BEC4BB903C0ACCAE79E769AAF5C9DBD678912EB4850F8095FB7E4F0643CF")]
    [InlineData("3bc2bec4bb903c0accae79e769aaf5c9dbd678912eb4850f8095fb7e4f0643cg")]
    public void Only_64_lower_case_hex_characters_are_a_fingerprint(string value)
    {
        Assert.False(Fingerprint.IsValid(value));
        Assert.Throws<ArgumentException>(() => new Fingerprint(value));
    }

    [Fact]
    public void The_fingerprint_does_not_contain_the_details()
    {
        var transaction = Transfer(-1234.56m, description: "abcdef", name: "Ján", account: "SK00");

        var value = One(transaction).Value;

        Assert.DoesNotContain("123456", value);
        Assert.DoesNotContain("abcdef", value);
    }
}
