using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Records;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Statements.Tests;

/// <summary>Reading transactions (spec: What a statement contains; How the printed layout maps to those fields).</summary>
public class TransactionTests
{
    private static readonly VubStatementReader Reader = new();

    public static TheoryData<string> Valid => new(Samples.Valid);

    [Theory]
    [MemberData(nameof(Valid))]
    public void Sample_reads_to_exactly_its_record(string name)
    {
        var result = Reader.Read(Samples.Pdf(name));

        Assert.True(result.IsSuccess, result.Rejection?.Message);
        Assert.Equal(RecordMapping.Canonical(Samples.Record(name)), RecordMapping.ToCanonicalJson(name, result.Statement!));
    }

    [Fact]
    public void A_january_statement_with_a_december_value_date_reads_with_the_right_years() =>
        AssertRoundTrip(Scenario("jan", new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31),
            Card(new DateOnly(2027, 1, 2), new DateOnly(2026, 12, 30), -12.40m)));

    [Fact]
    public void A_december_statement_with_a_booking_on_the_31st_reads_with_the_right_year() =>
        AssertRoundTrip(Scenario("dec", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31),
            Card(new DateOnly(2026, 12, 31), new DateOnly(2026, 12, 30), -8.00m)));

    [Fact]
    public void Two_identical_transactions_are_both_read()
    {
        var card = Card(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 9), -3.20m);
        var scenario = Scenario("twins", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), card, card);

        AssertRoundTrip(scenario);
        Assert.Equal(2, Reader.Read(VubStatementRenderer.Render(scenario)).Statement!.Transactions.Count);
    }

    [Fact]
    public void A_transfer_with_a_printed_value_date_reads_it_and_one_without_takes_the_booking_date()
    {
        var account = Iban.Slovak("1100", "000000", "1234567890");
        Transaction Transfer(DateOnly booked, DateOnly value) => new()
        {
            BookingDate = booked, ValueDate = value, Type = TransactionType.Transfer, Amount = -50.00m,
            CounterpartyAccount = account, CounterpartyName = "Peter Skúšobný", CounterpartyBic = "TATRSKBX",
        };

        AssertRoundTrip(Scenario("value-dates", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            Transfer(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 5)),
            Transfer(new DateOnly(2026, 6, 9), new DateOnly(2026, 6, 9))));
    }

    // New and unknown types (work/041-unknown-transaction-kinds/spec.md)

    [Fact]
    public void A_google_pay_payment_reads_as_a_wallet_card_payment() =>
        AssertRoundTrip(June(Card(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 8), -4.60m) with
        {
            Type = TransactionType.WalletCardPayment,
            GooglePay = true,
        }));

    [Fact]
    public void A_cash_withdrawal_reads_with_its_card_value_date_description_and_reference() =>
        AssertRoundTrip(June(Card(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 9), -50.00m) with
        {
            Type = TransactionType.CashWithdrawal,
            Description = "ATM00417 TRNAVA 10:15",
        }));

    [Fact]
    public void An_unknown_type_laid_out_like_a_card_payment_reads_with_its_printed_type_and_card()
    {
        var unknown = Card(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 9), -3.20m) with
        {
            Type = TransactionType.Unknown,
            PrintedType = "PLATBA HODINKAMI",
        };

        AssertRoundTrip(June(unknown));
        var read = Reader.Read(VubStatementRenderer.Render(June(unknown))).Statement!.Transactions.Single();
        Assert.Equal(Statements.Model.TransactionType.Unknown, read.Type);
        Assert.Equal("PLATBA HODINKAMI", read.PrintedType);
        Assert.Equal("1111", read.Card);
    }

    [Fact]
    public void An_unknown_type_laid_out_like_a_transfer_reads_its_symbols_and_details() =>
        AssertRoundTrip(June(new Transaction
        {
            BookingDate = new DateOnly(2026, 6, 12),
            ValueDate = new DateOnly(2026, 6, 11),
            Type = TransactionType.Unknown,
            PrintedType = "ZRÁŽKA ZO ZÁLOHY",
            Amount = -35.00m,
            Description = "Zaloha jun",
            CounterpartyName = "Správa Bytov Lipa",
            PaymentPurpose = "Zaloha jun",
            VariableSymbol = "6202",
            ConstantSymbol = "0308",
            Reference = "2606120000001",
        }));

    [Fact]
    public void A_long_printed_type_is_kept_whole() =>
        AssertRoundTrip(June(new Transaction
        {
            BookingDate = new DateOnly(2026, 6, 12),
            ValueDate = new DateOnly(2026, 6, 12),
            Type = TransactionType.Unknown,
            PrintedType = "PLATBA HODINKAMI ZA PRSTEN",
            Amount = -9.90m,
        }));

    [Fact]
    public void A_wallet_label_the_reader_does_not_know_stays_an_unknown_type() =>
        AssertRoundTrip(June(Card(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 9), -3.20m) with
        {
            Type = TransactionType.Unknown,
            PrintedType = "PLATBA KARTOU X-PAY",
        }));

    [Fact]
    public void Details_lines_with_labels_the_reader_does_not_know_are_accepted_and_dropped()
    {
        var transfer = new Transaction
        {
            BookingDate = new DateOnly(2026, 6, 8),
            ValueDate = new DateOnly(2026, 6, 8),
            Type = TransactionType.Transfer,
            Amount = 900.00m,
            CounterpartyAccount = Iban.Slovak("1100", "000000", "1234567890"),
            CounterpartyName = "Fiktívna Firma s.r.o.",
            CounterpartyBic = "TATRSKBX",
            PaymentPurpose = "Vyplata",
            Description = "Vyplata",
            OtherDetails =
            [
                ("Pôvodný odosielateľ:", "Fiktívna Materská Firma a.s."),
                // Long enough to wrap onto a line of its own.
                ("Konečný príjemca:", "TESTOVACÍ KLIENT ULICA 1 000 00 MESTO SLOVENSKÁ REPUBLIKA DLHÝ TEXT, KTORÝ SA ZALOMÍ NA ĎALŠÍ RIADOK"),
            ],
        };

        AssertRoundTrip(June(transfer));
    }

    [Fact]
    public void Known_types_carry_no_printed_type()
    {
        foreach (var name in Samples.Valid)
            Assert.All(Reader.Read(Samples.Pdf(name)).Statement!.Transactions,
                t => Assert.Equal(t.Type == Statements.Model.TransactionType.Unknown, t.PrintedType is not null));
    }

    [Fact]
    public void Reading_the_same_file_twice_gives_the_same_result()
    {
        var pdf = Samples.Pdf("01-typical-month");

        Assert.Equal(RecordMapping.ToCanonicalJson("x", Reader.Read(pdf).Statement!), RecordMapping.ToCanonicalJson("x", Reader.Read(pdf).Statement!));
    }

    private static void AssertRoundTrip(StatementScenario scenario)
    {
        var result = Reader.Read(VubStatementRenderer.Render(scenario));

        Assert.True(result.IsSuccess, result.Rejection?.Message);
        Assert.Equal(RecordMapping.Canonical(ExpectedRecord.ForValid(scenario)), RecordMapping.ToCanonicalJson(scenario.Name, result.Statement!));
    }

    private static Transaction Card(DateOnly booked, DateOnly value, decimal amount) => new()
    {
        BookingDate = booked,
        ValueDate = value,
        Type = TransactionType.CardPayment,
        Amount = amount,
        Card = "1111",
        Description = "OBCHOD NA ROHU",
        Reference = "0000000000001",
    };

    private static StatementScenario June(params Transaction[] transactions) =>
        Scenario("june", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), transactions);

    private static StatementScenario Scenario(string name, DateOnly start, DateOnly end, params Transaction[] transactions) => new()
    {
        Name = name,
        AccountHolder = "TESTOVACÍ KLIENT",
        HolderAddress = ["Ulica 1", "000 00 Mesto"],
        Iban = Iban.Slovak(FictionalBank.BankCode, "000000", "1111111111"),
        PeriodStart = start,
        PeriodEnd = end,
        OpeningBalance = 500.00m,
        Transactions = transactions,
    };
}
