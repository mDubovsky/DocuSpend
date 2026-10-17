using System.Text.Json;
using DocuSpend.SampleGenerator.Records;
using DocuSpend.Statements.Model;
using GeneratorType = DocuSpend.SampleGenerator.Model.TransactionType;

namespace DocuSpend.Statements.Tests;

/// <summary>
/// Puts a read statement into the shape of a sample's expected record, and writes both as canonical JSON:
/// every amount with exactly two decimals, so 0 and 0.00 compare equal. Amounts are compared as numbers and
/// every other field as text.
/// </summary>
public static class RecordMapping
{
    public static string Canonical(ExpectedRecord r) => RecordWriter.Serialize(r with
    {
        Statement = r.Statement is null ? null : Canonical(r.Statement),
        Transactions = r.Transactions?.Select(Canonical).ToList(),
    });

    public static string CanonicalJson(StatementRecord s) => JsonSerializer.Serialize(Canonical(s), RecordWriter.Options);

    public static string ToCanonicalJson(string sample, Statement s) => Canonical(new ExpectedRecord
    {
        Sample = sample,
        Expectation = "parse",
        Statement = StatementOnly(s),
        Transactions = s.Transactions.Select(ToRecord).ToList(),
    });

    public static StatementRecord StatementOnly(Statement s) => new()
    {
        AccountHolder = s.AccountHolder,
        Iban = s.Iban,
        Bic = s.Bic,
        Currency = s.Currency,
        PeriodStart = s.PeriodStart,
        PeriodEnd = s.PeriodEnd,
        OpeningBalance = s.OpeningBalance,
        ClosingBalance = s.ClosingBalance,
        TotalDeposits = s.TotalDeposits,
        TotalWithdrawals = s.TotalWithdrawals,
        TotalFees = s.TotalFees,
    };

    public static TransactionRecord ToRecord(Transaction t) => new()
    {
        BookingDate = t.BookingDate,
        ValueDate = t.ValueDate,
        Type = Enum.Parse<GeneratorType>(t.Type.ToString()),
        Amount = t.Amount,
        Currency = t.Currency,
        Card = t.Card,
        Description = t.Description,
        CounterpartyName = t.CounterpartyName,
        CounterpartyAccount = t.CounterpartyAccount,
        CounterpartyBic = t.CounterpartyBic,
        VariableSymbol = t.VariableSymbol,
        ConstantSymbol = t.ConstantSymbol,
        SpecificSymbol = t.SpecificSymbol,
        PayerReference = t.PayerReference,
        PaymentPurpose = t.PaymentPurpose,
        PurposeCategoryCode = t.PurposeCategoryCode,
        Reference = t.Reference,
        FeeType = t.FeeType,
        FeeAmount = t.FeeAmount,
    };

    private static StatementRecord Canonical(StatementRecord s) => s with
    {
        OpeningBalance = Cents(s.OpeningBalance),
        ClosingBalance = Cents(s.ClosingBalance),
        TotalDeposits = Cents(s.TotalDeposits),
        TotalWithdrawals = Cents(s.TotalWithdrawals),
        TotalFees = Cents(s.TotalFees),
    };

    private static TransactionRecord Canonical(TransactionRecord t) => t with
    {
        Amount = Cents(t.Amount),
        FeeAmount = t.FeeAmount is { } fee ? Cents(fee) : null,
    };

    /// <summary>The amount with exactly two decimals; more than two would itself be a reading error.</summary>
    private static decimal Cents(decimal d) =>
        decimal.Round(d, 2) == d ? decimal.Round(d, 2) + 0.00m : throw new InvalidDataException($"Not whole cents: {d}");
}
