namespace DocuSpend.Statements.Model;

/// <summary>
/// A bank statement for one period: its header, its printed summary totals and all its transactions.
/// Bank-neutral, so readers for other banks can produce it too.
/// </summary>
public sealed record Statement
{
    public required string AccountHolder { get; init; }

    /// <summary>The account number as printed, in groups of four.</summary>
    public required string Iban { get; init; }

    public required string Bic { get; init; }
    public required string Currency { get; init; }
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
    public required decimal OpeningBalance { get; init; }
    public required decimal ClosingBalance { get; init; }

    /// <summary>Money in, excluding fee rows.</summary>
    public required decimal TotalDeposits { get; init; }

    /// <summary>Money out, excluding fee rows; negative.</summary>
    public required decimal TotalWithdrawals { get; init; }

    /// <summary>Fees charged net of reversals; positive when charged.</summary>
    public required decimal TotalFees { get; init; }

    /// <summary>In the order the statement prints them.</summary>
    public required IReadOnlyList<Transaction> Transactions { get; init; }
}
