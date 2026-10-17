using DocuSpend.Statements.Model;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Statements.Tests;

public class ReadResultTests
{
    private static readonly Statement Empty = new()
    {
        AccountHolder = "A",
        Iban = "SK00",
        Bic = "B",
        Currency = "EUR",
        PeriodStart = new DateOnly(2026, 1, 1),
        PeriodEnd = new DateOnly(2026, 1, 31),
        OpeningBalance = 1m,
        ClosingBalance = 1m,
        TotalDeposits = 0m,
        TotalWithdrawals = 0m,
        TotalFees = 0m,
        Transactions = [],
    };

    [Fact]
    public void A_success_has_a_statement_and_no_rejection()
    {
        var result = ReadResult.Success(Empty);

        Assert.True(result.IsSuccess);
        Assert.Same(Empty, result.Statement);
        Assert.Null(result.Rejection);
    }

    [Fact]
    public void A_rejection_has_a_reason_and_no_statement()
    {
        var result = ReadResult.Reject(RejectionKind.EmptyFile, "The file is empty.");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Statement);
        Assert.Equal(RejectionKind.EmptyFile, result.Rejection!.Kind);
        Assert.Equal("The file is empty.", result.Rejection.Message);
    }

    [Fact]
    public void A_rejection_needs_a_message() =>
        Assert.Throws<ArgumentException>(() => ReadResult.Reject(RejectionKind.Unreadable, " "));
}
