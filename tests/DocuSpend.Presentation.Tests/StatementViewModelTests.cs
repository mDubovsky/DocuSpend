using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>What the statement view shows, checked against the real categorizer (spec: the statement view).</summary>
public class StatementViewModelTests
{
    public static TheoryData<string> Valid => new(Samples.Valid);

    private static StatementViewModel Show(string sample, CategorizationConfig? config = null)
    {
        var view = new StatementViewModel(config ?? CategorizationConfig.Default);
        view.Show(Samples.Read(sample));
        return view;
    }

    [Fact]
    public void The_header_shows_the_period_balances_and_spending()
    {
        var view = Show("01-typical-month");
        var expected = Categorizer.Categorize(Samples.Read("01-typical-month"), CategorizationConfig.Default);

        Assert.Equal(SlovakFormat.Period(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), view.Period);
        Assert.Equal(SlovakFormat.Amount(1486.20m), view.OpeningBalance);
        Assert.Equal(SlovakFormat.Amount(2041.19m), view.ClosingBalance);
        Assert.Equal(SlovakFormat.Amount(expected.SpendingTotal), view.SpendingTotal);
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void The_pie_has_exactly_the_spending_categories_with_spending_above_zero(string sample)
    {
        var view = Show(sample);
        var expected = Categorizer.Categorize(Samples.Read(sample), CategorizationConfig.Default).Categories
            .Where(c => c.Spending > 0)
            .ToDictionary(c => c.Category.Id, c => c.Spending!.Value);

        Assert.Equal(expected.Keys.Select(k => k.Value).Order(), view.Slices.Select(s => s.Category.Value).Order());
        Assert.All(view.Slices, s => Assert.Equal(expected[s.Category], s.Spending));
        Assert.Equal(view.Slices.OrderByDescending(s => s.Spending).Select(s => s.Category), view.Slices.Select(s => s.Category));
        if (view.Slices.Count > 0)
            Assert.Equal(1m, Math.Round(view.Slices.Sum(s => s.Share), 10));
        Assert.Equal(view.Slices.Count > 0, view.HasSpending);
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void The_category_list_has_every_category_with_transactions(string sample)
    {
        var view = Show(sample);
        var expected = Categorizer.Categorize(Samples.Read(sample), CategorizationConfig.Default).Categories
            .Where(c => c.Total.Count > 0)
            .ToList();

        Assert.Equal(expected.Select(c => c.Category.Id), view.Categories.Select(c => c.Category));
        Assert.All(view.Categories, row =>
        {
            var summary = expected.Single(c => c.Category.Id == row.Category);
            Assert.Equal(summary.Category.Name, row.Name);
            Assert.Equal(summary.Total.Count, row.Count);
            Assert.Equal(SlovakFormat.Amount(summary.Total.Amount), row.Total);
        });
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void The_balance_line_runs_from_the_opening_to_the_closing_balance_by_booking_date(string sample)
    {
        var statement = Samples.Read(sample);
        var points = Show(sample).BalancePoints;

        Assert.Equal(new BalancePoint(statement.PeriodStart, statement.OpeningBalance), points[0]);
        Assert.Equal(new BalancePoint(statement.PeriodEnd, statement.ClosingBalance), points[^1]);
        var running = statement.OpeningBalance;
        foreach (var day in statement.Transactions.GroupBy(t => t.BookingDate).OrderBy(g => g.Key))
        {
            running += day.Sum(t => t.Amount);
            Assert.Contains(new BalancePoint(day.Key, running), points);
        }
        Assert.Equal(points.OrderBy(p => p.Date).ToList(), points);
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void Selecting_a_category_lists_exactly_its_transactions_in_booking_date_order(string sample)
    {
        var view = Show(sample);
        var categorized = Categorizer.Categorize(Samples.Read(sample), CategorizationConfig.Default);

        foreach (var row in view.Categories)
        {
            view.SelectedCategory = row.Category;
            var expected = categorized.Transactions
                .Where(t => t.Placement.Category == row.Category)
                .OrderBy(t => t.Transaction.BookingDate)
                .ToList();

            Assert.Equal(expected.Select(t => t.Transaction), view.Transactions.Select(t => t.Transaction));
            Assert.All(view.Transactions, t => Assert.Equal(SlovakFormat.Amount(t.Transaction.Amount), t.Amount));
            Assert.All(view.Transactions, t => Assert.Equal(SlovakFormat.Date(t.Transaction.BookingDate), t.Date));
        }
    }

    [Fact]
    public void A_row_shows_the_description_or_else_the_counterparty_and_the_subcategory()
    {
        var view = Show("01-typical-month");

        foreach (var row in view.Categories)
        {
            view.SelectedCategory = row.Category;
            Assert.All(view.Transactions, t =>
            {
                Assert.Equal(t.Transaction.Description ?? t.Transaction.CounterpartyName ?? Texts.NoDescription, t.Description);
                Assert.False(string.IsNullOrWhiteSpace(t.Subcategory));
            });
        }
    }

    [Fact]
    public void The_selected_row_of_the_category_list_follows_the_selected_category_both_ways()
    {
        var view = Show("01-typical-month");
        var second = view.Categories[1];

        // Clicking a slice selects the category; the list follows.
        view.SelectedCategory = second.Category;
        Assert.Equal(second, view.SelectedRow);

        // Choosing a row in the list selects its category.
        view.SelectedRow = view.Categories[0];
        Assert.Equal(view.Categories[0].Category, view.SelectedCategory);

        view.SelectedCategory = null;
        Assert.Null(view.SelectedRow);
    }

    [Fact]
    public void Nothing_is_selected_at_first()
    {
        var view = Show("01-typical-month");

        Assert.Null(view.SelectedCategory);
        Assert.Empty(view.Transactions);
    }

    [Fact]
    public void An_empty_month_has_no_pie_no_categories_and_a_flat_line()
    {
        var view = Show("04-empty-month");

        Assert.False(view.HasSpending);
        Assert.Empty(view.Slices);
        Assert.Empty(view.Categories);
        Assert.Single(view.BalancePoints.Select(p => p.Balance).Distinct());
    }

    [Fact]
    public void Showing_another_statement_clears_the_selection()
    {
        var view = Show("01-typical-month");
        view.SelectedCategory = view.Categories[0].Category;

        view.Show(Samples.Read("02-busy-month"));

        Assert.Null(view.SelectedCategory);
        Assert.Empty(view.Transactions);
    }

    [Fact]
    public void A_new_configuration_categorizes_again_and_keeps_the_selection()
    {
        var view = Show("01-typical-month");
        var uncategorized = new CategoryId("uncategorized-out");
        view.SelectedCategory = uncategorized;
        Assert.NotEmpty(view.Transactions);

        // Move one of the uncategorized payments elsewhere with a user rule.
        var moved = view.Transactions[0].Transaction;
        view.Config = CategorizationConfig.Default
            .AddUserRuleOnTop(new TextCondition(moved.Description!, TextMode.EqualTo), new SubcategoryId("leisure.games")).Value!;

        Assert.Equal(uncategorized, view.SelectedCategory);
        Assert.DoesNotContain(view.Transactions, t => t.Transaction == moved);
        Assert.Contains(view.Categories, c => c.Category == new CategoryId("leisure"));
    }

    [Fact]
    public void A_category_whose_money_back_outweighs_its_spending_is_listed_but_not_in_the_pie()
    {
        var config = CategorizationConfig.Default
            .AddUserRuleOnTop(new TextCondition("LEKAREN", TextMode.Contains), new SubcategoryId("health.drugstore")).Value!;
        var view = new StatementViewModel(config);

        view.Show(Statement(
            Card(new DateOnly(2026, 3, 3), -10.00m, "LEKAREN POD HRADOM"),
            Card(new DateOnly(2026, 3, 9), 15.00m, "LEKAREN POD HRADOM"),
            Card(new DateOnly(2026, 3, 12), -20.00m, "PEKAREN KLAS")));

        var health = new CategoryId("health");
        Assert.DoesNotContain(view.Slices, s => s.Category == health);
        Assert.Contains(view.Categories, c => c.Category == health && c.Total == SlovakFormat.Amount(5.00m));
        view.SelectedCategory = health;
        Assert.Equal(2, view.Transactions.Count);
        Assert.Contains(view.Transactions, t => t.Amount == SlovakFormat.Amount(15.00m));
    }

    private static Transaction Card(DateOnly date, decimal amount, string merchant) => new()
    {
        BookingDate = date,
        ValueDate = date,
        Type = TransactionType.CardPayment,
        Amount = amount,
        Currency = "EUR",
        Description = merchant,
    };

    private static Statement Statement(params Transaction[] transactions) => new()
    {
        AccountHolder = "TEST",
        Iban = "SK00 0000 0000 0000 0000 0000",
        Bic = "TESTSKXX",
        Currency = "EUR",
        PeriodStart = new DateOnly(2026, 3, 1),
        PeriodEnd = new DateOnly(2026, 3, 31),
        OpeningBalance = 100m,
        ClosingBalance = 100m + transactions.Sum(t => t.Amount),
        TotalDeposits = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
        TotalWithdrawals = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount),
        TotalFees = 0m,
        Transactions = transactions,
    };
}
