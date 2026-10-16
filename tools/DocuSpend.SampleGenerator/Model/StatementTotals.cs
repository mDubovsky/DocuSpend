namespace DocuSpend.SampleGenerator.Model;

/// <summary>
/// Statement totals as printed in the page-1 balance summary.
/// Closing = opening + deposits + withdrawals - fees, where withdrawals are negative and fees positive when charged.
/// </summary>
public sealed record StatementTotals(
    decimal OpeningBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal TotalFees,
    decimal ClosingBalance)
{
    public static StatementTotals Compute(decimal openingBalance, IEnumerable<Transaction> transactions)
    {
        decimal deposits = 0m, withdrawals = 0m, fees = 0m;
        foreach (var tx in transactions)
        {
            if (tx.IsFee)
                fees -= tx.Amount;
            else if (tx.Amount >= 0)
                deposits += tx.Amount;
            else
                withdrawals += tx.Amount;
        }

        return new StatementTotals(openingBalance, deposits, withdrawals, fees,
            openingBalance + deposits + withdrawals - fees);
    }
}
