using System.Text.Json;
using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Records;
using DocuSpend.SampleGenerator.Rendering;

namespace DocuSpend.SampleGenerator.Tests;

public class ExpectedRecordTests
{
    private static StatementScenario Scenario(params Transaction[] transactions) => new()
    {
        Name = "test-month",
        AccountHolder = "TEST HOLDER",
        HolderAddress = ["Street 1", "000 00 Town"],
        Iban = "SK00 0000 0000 0000 0000 0000",
        PeriodStart = new DateOnly(2026, 9, 1),
        PeriodEnd = new DateOnly(2026, 9, 30),
        OpeningBalance = 100.00m,
        Transactions = transactions,
    };

    private static readonly Transaction Card = new()
    {
        BookingDate = new DateOnly(2026, 9, 3),
        ValueDate = new DateOnly(2026, 9, 2),
        Type = TransactionType.CardPayment,
        Amount = -12.30m,
        Card = "1234",
        Description = "SHOP TOWN",
        Reference = "0000000000001",
    };

    private static JsonElement Write(StatementScenario scenario) =>
        JsonDocument.Parse(RecordWriter.Serialize(ExpectedRecord.ForValid(scenario))).RootElement;

    [Fact]
    public void Valid_record_has_statement_fields_and_totals()
    {
        var root = Write(Scenario(Card));
        var statement = root.GetProperty("statement");

        Assert.Equal("test-month", root.GetProperty("sample").GetString());
        Assert.Equal("parse", root.GetProperty("expectation").GetString());
        Assert.Equal("TEST HOLDER", statement.GetProperty("accountHolder").GetString());
        Assert.Equal("SK00 0000 0000 0000 0000 0000", statement.GetProperty("iban").GetString());
        Assert.Equal(FictionalBank.Bic, statement.GetProperty("bic").GetString());
        Assert.Equal("EUR", statement.GetProperty("currency").GetString());
        Assert.Equal("2026-09-01", statement.GetProperty("periodStart").GetString());
        Assert.Equal("2026-09-30", statement.GetProperty("periodEnd").GetString());
        Assert.Equal(100.00m, statement.GetProperty("openingBalance").GetDecimal());
        Assert.Equal(87.70m, statement.GetProperty("closingBalance").GetDecimal());
        Assert.Equal(0m, statement.GetProperty("totalDeposits").GetDecimal());
        Assert.Equal(-12.30m, statement.GetProperty("totalWithdrawals").GetDecimal());
        Assert.Equal(0m, statement.GetProperty("totalFees").GetDecimal());
    }

    [Fact]
    public void Transactions_use_iso_dates_and_named_types()
    {
        var tx = Write(Scenario(Card)).GetProperty("transactions")[0];

        Assert.Equal("2026-09-03", tx.GetProperty("bookingDate").GetString());
        Assert.Equal("2026-09-02", tx.GetProperty("valueDate").GetString());
        Assert.Equal("cardPayment", tx.GetProperty("type").GetString());
        Assert.Equal(-12.30m, tx.GetProperty("amount").GetDecimal());
        Assert.Equal("1234", tx.GetProperty("card").GetString());
    }

    [Fact]
    public void Absent_fields_are_omitted_not_empty()
    {
        var tx = Write(Scenario(Card)).GetProperty("transactions")[0];

        Assert.False(tx.TryGetProperty("counterpartyName", out _));
        Assert.False(tx.TryGetProperty("variableSymbol", out _));
        Assert.False(tx.TryGetProperty("feeAmount", out _));
    }

    [Fact]
    public void Render_only_details_are_not_part_of_the_record()
    {
        var tx = Write(Scenario(Card with { ShowsCurrencyLine = true })).GetProperty("transactions")[0];

        Assert.False(tx.TryGetProperty("showsCurrencyLine", out _));
    }

    [Fact]
    public void Invalid_record_only_says_reject()
    {
        var root = JsonDocument.Parse(RecordWriter.Serialize(ExpectedRecord.ForInvalid("09-empty-file", "Zero-byte file"))).RootElement;

        Assert.Equal("reject", root.GetProperty("expectation").GetString());
        Assert.False(root.TryGetProperty("statement", out _));
        Assert.False(root.TryGetProperty("transactions", out _));
    }

    [Fact]
    public void Serialization_is_stable_and_uses_lf_line_endings()
    {
        var first = RecordWriter.Serialize(ExpectedRecord.ForValid(Scenario(Card)));
        var second = RecordWriter.Serialize(ExpectedRecord.ForValid(Scenario(Card)));

        Assert.Equal(first, second);
        Assert.DoesNotContain("\r", first);
        Assert.EndsWith("\n", first);
    }

    [Fact]
    public void Record_round_trips()
    {
        var json = RecordWriter.Serialize(ExpectedRecord.ForValid(Scenario(Card)));
        var record = RecordWriter.Deserialize(json);

        Assert.Equal("test-month", record.Sample);
        Assert.Single(record.Transactions!);
        Assert.Equal(-12.30m, record.Transactions![0].Amount);
        Assert.Equal(TransactionType.CardPayment, record.Transactions[0].Type);
    }
}
