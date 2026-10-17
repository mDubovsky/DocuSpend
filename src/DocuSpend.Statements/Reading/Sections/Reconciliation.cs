using DocuSpend.Statements.Model;

namespace DocuSpend.Statements.Reading.Sections;

/// <summary>
/// Checks a read statement against what it says about itself: the transactions' totals equal the printed totals,
/// and the closing balance follows from the opening balance and the totals. Fee rows count only in the fee total.
/// </summary>
internal static class Reconciliation
{
    /// <summary>The first disagreement, as a readable reason without amounts; null when everything adds up.</summary>
    public static string? Check(Statement s)
    {
        var regular = s.Transactions.Where(t => t.FeeAmount is null).ToList();
        var deposits = regular.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var withdrawals = regular.Where(t => t.Amount < 0).Sum(t => t.Amount);
        var fees = -s.Transactions.Where(t => t.FeeAmount is not null).Sum(t => t.Amount);

        if (deposits != s.TotalDeposits)
            return Disagrees("the total deposits");
        if (withdrawals != s.TotalWithdrawals)
            return Disagrees("the total withdrawals");
        if (fees != s.TotalFees)
            return Disagrees("the total fees");
        if (s.OpeningBalance + s.TotalDeposits + s.TotalWithdrawals - s.TotalFees != s.ClosingBalance)
            return "The statement doesn't add up: the closing balance doesn't follow from the opening balance and the totals in the balance summary.";
        return null;
    }

    private static string Disagrees(string total) =>
        $"The statement doesn't add up: {total} in the balance summary don't match the transactions read from the statement.";
}
