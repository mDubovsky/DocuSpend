namespace DocuSpend.SampleGenerator.Model;

/// <summary>The single description of one valid sample statement. Both the PDF and its record come from it.</summary>
public sealed record StatementScenario
{
    /// <summary>File name without extension, e.g. "01-typical-month".</summary>
    public required string Name { get; init; }

    public required string AccountHolder { get; init; }
    public required IReadOnlyList<string> HolderAddress { get; init; }
    public required string Iban { get; init; }
    public string Bic { get; init; } = Rendering.FictionalBank.Bic;
    public string Currency { get; init; } = "EUR";
    public string ProductName { get; init; } = Rendering.FictionalBank.ProductName;
    public string BranchAddress { get; init; } = "HLAVNÁ 12, TRNAVA";
    public string BranchCode { get; init; } = "41007";
    public string Distribution { get; init; } = "IB";

    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }
    public required decimal OpeningBalance { get; init; }

    public required IReadOnlyList<Transaction> Transactions { get; init; }

    /// <summary>The "Por. číslo" header value, e.g. "9/2026".</summary>
    public string StatementNumber => $"{PeriodEnd.Month}/{PeriodEnd.Year}";

    public DateOnly IssueDate => PeriodEnd;

    public StatementTotals Totals => StatementTotals.Compute(OpeningBalance, Transactions);
}
