using DocuSpend.SampleGenerator.Model;

namespace DocuSpend.SampleGenerator.Tests;

public class StatementTotalsTests
{
    private static readonly DateOnly Day = new(2026, 9, 10);

    private static Transaction Tx(TransactionType type, decimal amount) => new()
    {
        BookingDate = Day,
        ValueDate = Day,
        Type = type,
        Amount = amount,
        FeeAmount = type is TransactionType.AccountFee or TransactionType.FeeReversal ? amount : null,
    };

    [Fact]
    public void Deposits_and_withdrawals_are_summed_by_sign()
    {
        var totals = StatementTotals.Compute(100.00m,
        [
            Tx(TransactionType.Transfer, 1500.00m),
            Tx(TransactionType.CardPayment, -20.50m),
            Tx(TransactionType.InstantTransfer, -79.50m),
            Tx(TransactionType.Transfer, 250.00m),
        ]);

        Assert.Equal(1750.00m, totals.TotalDeposits);
        Assert.Equal(-100.00m, totals.TotalWithdrawals);
        Assert.Equal(0.00m, totals.TotalFees);
        Assert.Equal(1750.00m, totals.ClosingBalance);
    }

    [Fact]
    public void Fees_are_counted_separately_and_reduce_the_balance()
    {
        var totals = StatementTotals.Compute(50.00m,
        [
            Tx(TransactionType.CardPayment, -10.00m),
            Tx(TransactionType.AccountFee, -4.00m),
        ]);

        Assert.Equal(0.00m, totals.TotalDeposits);
        Assert.Equal(-10.00m, totals.TotalWithdrawals);
        Assert.Equal(4.00m, totals.TotalFees);
        Assert.Equal(36.00m, totals.ClosingBalance);
    }

    [Fact]
    public void A_fee_and_its_reversal_net_to_zero()
    {
        var totals = StatementTotals.Compute(10.00m,
        [
            Tx(TransactionType.AccountFee, -7.00m),
            Tx(TransactionType.FeeReversal, 7.00m),
        ]);

        Assert.Equal(0.00m, totals.TotalFees);
        Assert.Equal(10.00m, totals.ClosingBalance);
    }

    [Fact]
    public void An_empty_month_keeps_the_opening_balance()
    {
        var totals = StatementTotals.Compute(321.09m, []);

        Assert.Equal(321.09m, totals.ClosingBalance);
        Assert.Equal(0m, totals.TotalDeposits);
        Assert.Equal(0m, totals.TotalWithdrawals);
    }

    [Fact]
    public void Closing_balance_reconciles_with_the_totals()
    {
        var totals = StatementTotals.Compute(1000.00m,
        [
            Tx(TransactionType.Transfer, 300.00m),
            Tx(TransactionType.WalletCardPayment, -45.67m),
            Tx(TransactionType.AccountFee, -7.00m),
        ]);

        Assert.Equal(
            totals.OpeningBalance + totals.TotalDeposits + totals.TotalWithdrawals - totals.TotalFees,
            totals.ClosingBalance);
    }
}
