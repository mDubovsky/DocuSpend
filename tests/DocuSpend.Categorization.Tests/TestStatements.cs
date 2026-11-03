using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Tests;

/// <summary>Builds statements around transactions for tests; the totals follow from the transactions.</summary>
internal static class TestStatements
{
    public static Statement Of(params Transaction[] transactions) =>
        Of(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), transactions);

    public static Statement Of(DateOnly start, DateOnly end, params Transaction[] transactions)
    {
        var regular = transactions.Where(t => t.FeeAmount is null).ToList();
        var deposits = regular.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var withdrawals = regular.Where(t => t.Amount < 0).Sum(t => t.Amount);
        var fees = -transactions.Where(t => t.FeeAmount is not null).Sum(t => t.Amount);
        const decimal opening = 1000m;
        return new Statement
        {
            AccountHolder = "TEST",
            Iban = "SK00",
            Bic = "TESTSKXX",
            Currency = "EUR",
            PeriodStart = start,
            PeriodEnd = end,
            OpeningBalance = opening,
            ClosingBalance = opening + deposits + withdrawals - fees,
            TotalDeposits = deposits,
            TotalWithdrawals = withdrawals,
            TotalFees = fees,
            Transactions = transactions,
        };
    }
}
