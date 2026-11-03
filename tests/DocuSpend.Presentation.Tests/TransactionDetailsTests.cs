using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>What an open row shows about its transaction (work/045-transaction-details/spec.md: what the details show).</summary>
public class TransactionDetailsTests
{
    public static TheoryData<string> Valid => new(Samples.Valid);

    private static readonly Transaction Full = new()
    {
        BookingDate = new DateOnly(2026, 5, 3),
        ValueDate = new DateOnly(2026, 5, 2),
        Type = TransactionType.Transfer,
        Amount = -440.93m,
        Currency = "EUR",
        Card = "2178",
        Description = "PLATBA FAKTURY",
        CounterpartyName = "Telekomunikácie Sever a.s.",
        CounterpartyAccount = "SK07 9900 0000 0018 7346 5290",
        CounterpartyBic = "VZORSKXX",
        VariableSymbol = "50484290",
        ConstantSymbol = "0308",
        SpecificSymbol = "2715173768",
        PayerReference = "/VS50484290/SS/KS0308",
        PaymentPurpose = "PLATBA FAKTURY ZA MAJ 2026",
        PurposeCategoryCode = "SALA",
        Reference = "2605033BFAFUB",
        FeeType = "E",
        FeeAmount = -440.93m,
    };

    private static List<(string Label, string Value)> Pairs(Transaction transaction) =>
        TransactionDetails.Of(transaction).Select(d => (d.Label, d.Value)).ToList();

    private static List<string> Labels(Transaction transaction) => TransactionDetails.Of(transaction).Select(d => d.Label).ToList();

    private static Transaction FromSample(string sample, Func<Transaction, bool> which) => Samples.Read(sample).Transactions.First(which);

    [Fact]
    public void Every_detail_shows_in_the_specs_order_with_its_label()
    {
        Assert.Equal(
        [
            ("Protistrana", "Telekomunikácie Sever a.s."),
            ("Účet protistrany", "SK07 9900 0000 0018 7346 5290"),
            ("BIC banky protistrany", "VZORSKXX"),
            ("Popis", "PLATBA FAKTURY"),
            ("Účel platby", "PLATBA FAKTURY ZA MAJ 2026"),
            ("Variabilný symbol", "50484290"),
            ("Konštantný symbol", "0308"),
            ("Špecifický symbol", "2715173768"),
            ("Referencia platiteľa", "/VS50484290/SS/KS0308"),
            ("Typ transakcie", "Prevod"),
            ("Karta", "**** 2178"),
            ("Dátum zaúčtovania", SlovakFormat.Date(new DateOnly(2026, 5, 3))),
            ("Dátum valuty", SlovakFormat.Date(new DateOnly(2026, 5, 2))),
            ("Kód účelu platby", "SALA"),
            ("Kód poplatku", "E"),
            ("Referencia transakcie", "2605033BFAFUB"),
        ], Pairs(Full));
    }

    [Fact]
    public void Each_group_after_the_first_starts_with_space_above_it()
    {
        var starts = TransactionDetails.Of(Full).Where(d => d.StartsGroup).Select(d => d.Label);

        Assert.Equal(["Popis", "Typ transakcie", "Dátum zaúčtovania", "Kód účelu platby"], starts);
    }

    [Fact]
    public void A_group_missing_its_first_details_starts_at_the_first_one_shown()
    {
        // A card payment has no counterparty: its first group is "what for", which then needs no space above it.
        var card = FromSample("02-busy-month", t => t.Type == TransactionType.CardPayment);

        var starts = TransactionDetails.Of(card).Where(d => d.StartsGroup).Select(d => d.Label);

        Assert.Equal(["Typ transakcie", "Dátum zaúčtovania", "Referencia transakcie"], starts);
    }

    [Fact]
    public void A_transfer_from_the_busy_month_shows_what_it_has()
    {
        var transfer = FromSample("02-busy-month", t => t.SpecificSymbol is not null && t.Type == TransactionType.Transfer);

        Assert.Equal(
            ["Protistrana", "Účet protistrany", "BIC banky protistrany", "Popis", "Variabilný symbol", "Konštantný symbol",
             "Špecifický symbol", "Typ transakcie", "Dátum zaúčtovania", "Dátum valuty", "Referencia transakcie"],
            Labels(transfer));
    }

    [Fact]
    public void A_card_payment_shows_its_card_and_no_counterparty()
    {
        var card = FromSample("02-busy-month", t => t.Type == TransactionType.CardPayment);

        var pairs = Pairs(card);

        Assert.Contains(("Karta", "**** " + card.Card), pairs);
        Assert.Equal(
            ["Popis", "Typ transakcie", "Karta", "Dátum zaúčtovania", "Dátum valuty", "Referencia transakcie"],
            Labels(card));
    }

    [Fact]
    public void A_purpose_equal_to_the_description_shows_once()
    {
        var same = Full with { PaymentPurpose = Full.Description };

        Assert.DoesNotContain("Účel platby", Labels(same));
        Assert.Contains("Popis", Labels(same));
    }

    [Fact]
    public void A_purpose_longer_than_the_cut_description_shows_beside_it()
    {
        // 03-awkward-month prints a description cut short and the purpose in full.
        var roof = FromSample("03-awkward-month", t => t.PaymentPurpose?.Contains("strechy", StringComparison.Ordinal) == true);

        var pairs = Pairs(roof);

        Assert.Contains(("Popis", roof.Description!), pairs);
        Assert.Contains(("Účel platby", roof.PaymentPurpose!), pairs);
    }

    [Fact]
    public void The_payers_reference_shows()
    {
        var paid = FromSample("03-awkward-month", t => t.PayerReference is not null);

        Assert.Contains(("Referencia platiteľa", paid.PayerReference!), Pairs(paid));
    }

    [Fact]
    public void A_transfer_with_only_an_account_shows_only_that_type_and_dates()
    {
        var bare = FromSample("03-awkward-month", t => t.Description is null && t.CounterpartyName is null);

        Assert.Equal(["Účet protistrany", "Typ transakcie", "Dátum zaúčtovania", "Dátum valuty"], Labels(bare));
    }

    [Fact]
    public void An_unknown_type_shows_the_type_as_printed()
    {
        foreach (var unknown in Samples.Read("10-new-types-month").Transactions.Where(t => t.Type == TransactionType.Unknown))
            Assert.Contains(("Typ transakcie", unknown.PrintedType!), Pairs(unknown));
    }

    [Fact]
    public void A_fee_shows_its_symbols_and_never_repeats_the_amount()
    {
        var fee = FromSample("02-busy-month", t => t.Type == TransactionType.AccountFee);

        var pairs = Pairs(fee);

        Assert.Contains(("Variabilný symbol", fee.VariableSymbol!), pairs);
        Assert.Contains(("Konštantný symbol", fee.ConstantSymbol!), pairs);
        Assert.DoesNotContain(pairs, p => p.Value == SlovakFormat.Amount(fee.Amount) || p.Value.Contains("6,90", StringComparison.Ordinal));
    }

    public static TheoryData<TransactionType> KnownTypes => new(Enum.GetValues<TransactionType>().Where(t => t != TransactionType.Unknown));

    [Theory]
    [MemberData(nameof(KnownTypes))]
    public void A_known_type_shows_its_slovak_name(TransactionType type) =>
        Assert.Contains(("Typ transakcie", Texts.TypeName(type)), Pairs(Full with { Type = type }));

    [Fact]
    public void A_transaction_with_only_a_type_dates_and_an_amount_still_has_details()
    {
        var bare = new Transaction
        {
            BookingDate = new DateOnly(2026, 5, 31),
            ValueDate = new DateOnly(2026, 5, 31),
            Type = TransactionType.AccountFee,
            Amount = -6.90m,
            Currency = "EUR",
        };

        Assert.Equal(["Typ transakcie", "Dátum zaúčtovania", "Dátum valuty"], Labels(bare));
    }

    [Fact]
    public void An_empty_detail_is_left_out()
    {
        var empty = Full with { CounterpartyBic = "", PayerReference = "   " };

        Assert.DoesNotContain("BIC banky protistrany", Labels(empty));
        Assert.DoesNotContain("Referencia platiteľa", Labels(empty));
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void Every_sample_transaction_shows_each_of_its_details_once(string sample)
    {
        foreach (var t in Samples.Read(sample).Transactions)
        {
            var details = TransactionDetails.Of(t);
            string?[] optional =
            [
                t.CounterpartyName, t.CounterpartyAccount, t.CounterpartyBic, t.Description,
                t.PaymentPurpose == t.Description ? null : t.PaymentPurpose,
                t.VariableSymbol, t.ConstantSymbol, t.SpecificSymbol, t.PayerReference, t.Card,
                t.PurposeCategoryCode, t.FeeType, t.Reference,
            ];

            Assert.Equal(optional.Count(v => !string.IsNullOrWhiteSpace(v)) + 3, details.Count);
            Assert.Equal(details.Count, details.Select(d => d.Label).Distinct().Count());
            Assert.All(details, d => Assert.False(string.IsNullOrWhiteSpace(d.Value)));
            Assert.DoesNotContain(details, d => d.Value == SlovakFormat.Amount(t.Amount));
        }
    }
}
