using DocuSpend.SampleGenerator.Model;

namespace DocuSpend.SampleGenerator.Records;

/// <summary>The companion record of a sample: what a correct reader must get out of the PDF.</summary>
public sealed record ExpectedRecord
{
    public required string Sample { get; init; }

    /// <summary>"parse" for valid statements, "reject" for files that are not readable VÚB statements.</summary>
    public required string Expectation { get; init; }

    public string? Reason { get; init; }
    public StatementRecord? Statement { get; init; }
    public IReadOnlyList<TransactionRecord>? Transactions { get; init; }

    public static ExpectedRecord ForValid(StatementScenario scenario)
    {
        var totals = scenario.Totals;
        return new ExpectedRecord
        {
            Sample = scenario.Name,
            Expectation = "parse",
            Statement = new StatementRecord
            {
                AccountHolder = scenario.AccountHolder,
                Iban = scenario.Iban,
                Bic = scenario.Bic,
                Currency = scenario.Currency,
                PeriodStart = scenario.PeriodStart,
                PeriodEnd = scenario.PeriodEnd,
                OpeningBalance = totals.OpeningBalance,
                ClosingBalance = totals.ClosingBalance,
                TotalDeposits = totals.TotalDeposits,
                TotalWithdrawals = totals.TotalWithdrawals,
                TotalFees = totals.TotalFees,
            },
            Transactions = scenario.Transactions.Select(TransactionRecord.From).ToList(),
        };
    }

    public static ExpectedRecord ForInvalid(string sample, string reason) => new()
    {
        Sample = sample,
        Expectation = "reject",
        Reason = reason,
    };
}

public sealed record StatementRecord
{
    public required string AccountHolder { get; init; }
    public required string Iban { get; init; }
    public required string Bic { get; init; }
    public required string Currency { get; init; }
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
    public required decimal OpeningBalance { get; init; }
    public required decimal ClosingBalance { get; init; }
    public required decimal TotalDeposits { get; init; }
    public required decimal TotalWithdrawals { get; init; }
    public required decimal TotalFees { get; init; }
}

public sealed record TransactionRecord
{
    public required DateOnly BookingDate { get; init; }
    public required DateOnly ValueDate { get; init; }
    public required TransactionType Type { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public string? Card { get; init; }
    public string? Description { get; init; }
    public string? CounterpartyName { get; init; }
    public string? CounterpartyAccount { get; init; }
    public string? CounterpartyBic { get; init; }
    public string? VariableSymbol { get; init; }
    public string? ConstantSymbol { get; init; }
    public string? SpecificSymbol { get; init; }
    public string? PayerReference { get; init; }
    public string? PaymentPurpose { get; init; }
    public string? PurposeCategoryCode { get; init; }
    public string? Reference { get; init; }
    public string? FeeType { get; init; }
    public decimal? FeeAmount { get; init; }

    public static TransactionRecord From(Transaction tx) => new()
    {
        BookingDate = tx.BookingDate,
        ValueDate = tx.ValueDate,
        Type = tx.Type,
        Amount = tx.Amount,
        Currency = tx.Currency,
        Card = tx.Card,
        Description = tx.Description,
        CounterpartyName = tx.CounterpartyName,
        CounterpartyAccount = tx.CounterpartyAccount,
        CounterpartyBic = tx.CounterpartyBic,
        VariableSymbol = tx.VariableSymbol,
        ConstantSymbol = tx.ConstantSymbol,
        SpecificSymbol = tx.SpecificSymbol,
        PayerReference = tx.PayerReference,
        PaymentPurpose = tx.PaymentPurpose,
        PurposeCategoryCode = tx.PurposeCategoryCode,
        Reference = tx.Reference,
        FeeType = tx.FeeType,
        FeeAmount = tx.FeeAmount,
    };
}
