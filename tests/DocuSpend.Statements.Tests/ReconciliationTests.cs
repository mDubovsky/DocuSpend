using DocuSpend.SampleGenerator.Model;
using DocuSpend.SampleGenerator.Rendering;
using DocuSpend.SampleGenerator.Scenarios;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Statements.Tests;

/// <summary>
/// A statement reads completely or not at all (spec: A statement reads completely or not at all; Rejections).
/// Inconsistent and damaged statements are built with the generator's test-only render options.
/// </summary>
public class ReconciliationTests
{
    private static readonly VubStatementReader Reader = new();

    private static readonly StatementScenario Two = new()
    {
        Name = "two-cards",
        AccountHolder = "TESTOVACÍ KLIENT",
        HolderAddress = ["Ulica 1", "000 00 Mesto"],
        Iban = Iban.Slovak(FictionalBank.BankCode, "000000", "2222222222"),
        PeriodStart = new DateOnly(2026, 6, 1),
        PeriodEnd = new DateOnly(2026, 6, 30),
        OpeningBalance = 100.00m,
        Transactions =
        [
            Card(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 9), -12.40m),
            Card(new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 19), -3.20m),
        ],
    };

    private static Rejection Rejected(RenderOptions options)
    {
        var result = Reader.Read(VubStatementRenderer.Render(Two, options));
        Assert.False(result.IsSuccess, "expected a rejection");
        return result.Rejection!;
    }

    [Fact]
    public void The_scenario_itself_reads()
    {
        Assert.True(Reader.Read(VubStatementRenderer.Render(Two)).IsSuccess);
    }

    [Theory]
    [InlineData("deposits")]
    [InlineData("withdrawals")]
    [InlineData("fees")]
    public void A_printed_total_that_disagrees_with_the_transactions_is_rejected(string total)
    {
        var t = Two.Totals;
        var wrong = total switch
        {
            "deposits" => t with { TotalDeposits = 5.00m, ClosingBalance = t.ClosingBalance + 5.00m },
            "withdrawals" => t with { TotalWithdrawals = -20.00m, ClosingBalance = t.ClosingBalance - 4.40m },
            _ => t with { TotalFees = 1.00m, ClosingBalance = t.ClosingBalance - 1.00m },
        };

        var rejection = Rejected(new RenderOptions { PrintedTotals = wrong });

        Assert.Equal(RejectionKind.DoesNotReconcile, rejection.Kind);
        Assert.Contains(total, rejection.Message);
    }

    [Fact]
    public void A_closing_balance_that_does_not_follow_is_rejected()
    {
        var rejection = Rejected(new RenderOptions { PrintedTotals = Two.Totals with { ClosingBalance = 1.00m } });

        Assert.Equal(RejectionKind.DoesNotReconcile, rejection.Kind);
        Assert.Contains("closing balance", rejection.Message);
    }

    [Fact]
    public void A_statement_without_its_summary_totals_is_rejected_naming_the_balance_summary()
    {
        string[] labels = ["Vlastné použiteľné prostriedky", "Vklady spolu", "Výbery spolu", "Poplatky spolu"];
        var rejection = Rejected(new RenderOptions { TextTransform = t => labels.Contains(t) || t.StartsWith("Vlast. použit.", StringComparison.Ordinal) ? null : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
        Assert.Contains("balance summary", rejection.Message);
    }

    [Fact]
    public void A_damaged_amount_is_rejected_naming_the_page_and_transaction()
    {
        var rejection = Rejected(new RenderOptions { TextTransform = t => t == "-12,40" ? "-12,4X" : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
        Assert.Contains("Transaction 1 on page 1", rejection.Message);
        Assert.Contains("amount", rejection.Message);
    }

    [Fact]
    public void A_damaged_value_date_is_rejected_naming_the_page_and_transaction()
    {
        var rejection = Rejected(new RenderOptions { TextTransform = t => t == "19/06" ? "19/0X" : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
        Assert.Contains("Transaction 2 on page 1", rejection.Message);
        Assert.Contains("value date", rejection.Message);
    }

    [Fact]
    public void A_damaged_booking_date_is_rejected_naming_the_page_and_transaction()
    {
        var rejection = Rejected(new RenderOptions { TextTransform = t => t == "20/06" ? "2O/06" : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
        Assert.Contains("Transaction 2 on page 1", rejection.Message);
        Assert.Contains("booking date", rejection.Message);
    }

    // An unknown type reads, but a damaged one is rejected like any other (work/041-unknown-transaction-kinds/spec.md).

    private static readonly StatementScenario WithUnknown = Two with
    {
        Name = "with-unknown",
        Transactions =
        [
            Two.Transactions[0],
            Card(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 14), -7.30m) with { Type = TransactionType.Unknown, PrintedType = "PLATBA HODINKAMI" },
        ],
    };

    private static Rejection RejectedWithUnknown(RenderOptions options)
    {
        var result = Reader.Read(VubStatementRenderer.Render(WithUnknown, options));
        Assert.False(result.IsSuccess, "expected a rejection");
        return result.Rejection!;
    }

    [Fact]
    public void The_scenario_with_an_unknown_type_reads() =>
        Assert.True(Reader.Read(VubStatementRenderer.Render(WithUnknown)).IsSuccess);

    [Fact]
    public void An_unknown_type_without_an_amount_is_rejected_naming_the_page_and_transaction()
    {
        var rejection = RejectedWithUnknown(new RenderOptions { TextTransform = t => t == "-7,30" ? null : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
        Assert.Contains("Transaction 2 on page 1", rejection.Message);
        Assert.Contains("amount", rejection.Message);
    }

    [Fact]
    public void An_unknown_type_with_a_damaged_booking_date_is_rejected()
    {
        var rejection = RejectedWithUnknown(new RenderOptions { TextTransform = t => t == "15/06" ? "1S/06" : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
        Assert.Contains("booking date", rejection.Message);
    }

    [Fact]
    public void A_statement_with_an_unknown_type_that_does_not_add_up_is_rejected()
    {
        var rejection = RejectedWithUnknown(new RenderOptions { PrintedTotals = Two.Totals });

        Assert.Equal(RejectionKind.DoesNotReconcile, rejection.Kind);
    }

    [Fact]
    public void A_line_at_the_left_edge_that_is_neither_a_date_nor_a_label_is_rejected()
    {
        // The second transaction's booking date damaged into something that is no label either.
        var rejection = Rejected(new RenderOptions { TextTransform = t => t == "20/06" ? "Platba" : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
        Assert.Contains("booking date", rejection.Message);
    }

    [Fact]
    public void A_damaged_date_that_looks_like_a_label_word_is_still_rejected()
    {
        var rejection = Rejected(new RenderOptions { TextTransform = t => t == "20/06" ? "20/06:" : t });

        Assert.Equal(RejectionKind.Unreadable, rejection.Kind);
    }

    [Fact]
    public void Rejection_messages_quote_no_content()
    {
        var rejection = Rejected(new RenderOptions { PrintedTotals = Two.Totals with { TotalWithdrawals = -20.00m } });

        Assert.DoesNotContain("20,00", rejection.Message);
        Assert.DoesNotContain("15,60", rejection.Message);
        Assert.DoesNotContain("TESTOVACÍ", rejection.Message);
    }

    private static Transaction Card(DateOnly booked, DateOnly value, decimal amount) => new()
    {
        BookingDate = booked,
        ValueDate = value,
        Type = TransactionType.CardPayment,
        Amount = amount,
        Card = "1111",
        Description = "OBCHOD NA ROHU",
        Reference = "0000000000001",
    };
}
