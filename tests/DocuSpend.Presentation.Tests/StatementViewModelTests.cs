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

        Assert.Equal(expected.Select(c => c.Category.Id.Value).Order(), view.Categories.Select(c => c.Category.Value).Order());
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
    public void Every_day_of_the_period_has_the_balance_at_the_end_of_that_day(string sample)
    {
        var statement = Samples.Read(sample);
        var points = Show(sample).BalancePoints;

        var days = statement.PeriodEnd.DayNumber - statement.PeriodStart.DayNumber + 1;
        Assert.Equal(days, points.Count);
        for (var i = 0; i < days; i++)
        {
            var date = statement.PeriodStart.AddDays(i);
            var expected = statement.OpeningBalance + statement.Transactions.Where(t => t.BookingDate <= date).Sum(t => t.Amount);
            Assert.Equal(date, points[i].Date);
            Assert.Equal(expected, points[i].Balance);
            Assert.Equal(SlovakFormat.Date(date), points[i].DateText);
            Assert.Equal(SlovakFormat.Amount(expected), points[i].BalanceText);
        }
        Assert.Equal(statement.ClosingBalance, points[^1].Balance);
    }

    [Fact]
    public void A_booking_on_the_first_day_shows_on_the_first_day()
    {
        var statement = Samples.Read("02-busy-month");
        var points = Show("02-busy-month").BalancePoints;

        var onFirstDay = statement.Transactions.Where(t => t.BookingDate == statement.PeriodStart).Sum(t => t.Amount);
        Assert.NotEqual(0m, onFirstDay);
        Assert.Equal(statement.OpeningBalance + onFirstDay, points[0].Balance);
    }

    [Fact]
    public void A_day_without_bookings_carries_the_balance_over_and_a_day_with_several_shows_the_balance_after_all_of_them()
    {
        var statement = Samples.Read("02-busy-month");
        var points = Show("02-busy-month").BalancePoints;

        var several = statement.Transactions.GroupBy(t => t.BookingDate).First(g => g.Count() > 1).Key;
        var after = points.Single(p => p.Date == several).Balance;
        Assert.Equal(statement.OpeningBalance + statement.Transactions.Where(t => t.BookingDate <= several).Sum(t => t.Amount), after);

        var quietStatement = Samples.Read("01-typical-month");
        var quietPoints = Show("01-typical-month").BalancePoints;
        var quiet = quietPoints.Select((p, i) => (p, i)).First(x => x.i > 0 && quietStatement.Transactions.All(t => t.BookingDate != x.p.Date));
        Assert.Equal(quietPoints[quiet.i - 1].Balance, quiet.p.Balance);
    }

    [Fact]
    public void In_a_month_with_one_transaction_the_balance_changes_on_its_day_only()
    {
        var statement = Samples.Read("05-single-transaction-month");
        var points = Show("05-single-transaction-month").BalancePoints;

        var day = statement.Transactions.Single().BookingDate;
        Assert.All(points.Where(p => p.Date < day), p => Assert.Equal(statement.OpeningBalance, p.Balance));
        Assert.All(points.Where(p => p.Date >= day), p => Assert.Equal(statement.ClosingBalance, p.Balance));
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void Selecting_a_category_lists_exactly_its_transactions(string sample)
    {
        var view = Show(sample);
        var categorized = Categorizer.Categorize(Samples.Read(sample), CategorizationConfig.Default);

        foreach (var row in view.Categories)
        {
            view.SelectedCategory = row.Category;
            var expected = categorized.Transactions
                .Select((t, index) => (t, index))
                .Where(x => x.t.Placement.Category == row.Category)
                .ToList();

            Assert.Equal(expected.Select(x => x.index), view.Transactions.Select(t => t.Index).Order());
            Assert.All(view.Transactions, t => Assert.Equal(categorized.Transactions[t.Index].Transaction, t.Transaction));
            Assert.All(view.Transactions, t => Assert.Equal(SlovakFormat.Amount(t.Transaction.Amount), t.Amount));
            Assert.All(view.Transactions, t => Assert.Equal(SlovakFormat.Date(t.Transaction.BookingDate), t.Date));
        }
    }

    // Largest first (work/047-largest-first/spec.md)

    [Theory]
    [MemberData(nameof(Valid))]
    public void The_category_list_is_by_size_largest_first_and_equal_sizes_in_the_screens_order(string sample)
    {
        var view = Show(sample);
        var screens = CatalogOrder.Categories(view.Config.Catalog).Select(c => c.Id).ToList();

        foreach (var (first, second) in view.Categories.Zip(view.Categories.Skip(1)))
        {
            Assert.True(Math.Abs(first.Amount) >= Math.Abs(second.Amount), $"{first.Name} before {second.Name}");
            if (Math.Abs(first.Amount) == Math.Abs(second.Amount))
                Assert.True(screens.IndexOf(first.Category) < screens.IndexOf(second.Category), $"{first.Name} before {second.Name}");
        }
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void Each_categorys_transactions_are_by_size_then_booking_date_then_the_statements_order(string sample)
    {
        var view = Show(sample);

        foreach (var row in view.Categories)
        {
            view.SelectedCategory = row.Category;
            foreach (var (first, second) in view.Transactions.Zip(view.Transactions.Skip(1)))
            {
                var (a, b) = (Math.Abs(first.Transaction.Amount), Math.Abs(second.Transaction.Amount));
                Assert.True(a >= b);
                if (a == b)
                {
                    Assert.True(first.Transaction.BookingDate <= second.Transaction.BookingDate);
                    if (first.Transaction.BookingDate == second.Transaction.BookingDate)
                        Assert.True(first.Index < second.Index);
                }
            }
        }
    }

    [Fact]
    public void Transactions_are_compared_by_size_whichever_way_the_money_moved()
    {
        var view = new StatementViewModel(RuleFor("ZZMIX", "health.drugstore"));
        view.Show(Statement(
            Card(new DateOnly(2026, 3, 10), -50.00m, "ZZMIX A"),
            Card(new DateOnly(2026, 3, 5), 200.00m, "ZZMIX B"),
            Card(new DateOnly(2026, 3, 20), -500.00m, "ZZMIX C"),
            Card(new DateOnly(2026, 3, 25), 45.00m, "ZZMIX D"),
            Card(new DateOnly(2026, 3, 12), 45.00m, "ZZMIX E"),
            Card(new DateOnly(2026, 3, 12), -45.00m, "ZZMIX F"),
            Card(new DateOnly(2026, 3, 1), 0.00m, "ZZMIX G")));

        view.SelectedCategory = new CategoryId("health");

        // Equal sizes: the earlier date first, out and in alike; on the same date, as the statement lists them.
        Assert.Equal(["C", "B", "A", "E", "F", "D", "G"], view.Transactions.Select(t => t.Transaction.Description![^1..]));
    }

    [Fact]
    public void Categories_are_compared_by_the_size_of_their_total_with_uncategorized_among_them_and_zero_last()
    {
        var config = RuleFor("ZZPLAT", "income.salary", "ZZBYT", "housing.rent", "ZZJEDLO", "food.groceries", "ZZLEK", "health.drugstore");
        var view = new StatementViewModel(config);
        view.Show(Statement(
            Card(new DateOnly(2026, 3, 2), -30.00m, "ZZLEK"),
            Card(new DateOnly(2026, 3, 3), -412.00m, "ZZJEDLO"),
            Card(new DateOnly(2026, 3, 4), -260.00m, "NIEKDE"),
            Card(new DateOnly(2026, 3, 5), 1850.00m, "ZZPLAT"),
            Card(new DateOnly(2026, 3, 6), -732.40m, "ZZBYT"),
            Card(new DateOnly(2026, 3, 7), 12.00m, "ODNIEKIAL"),
            Card(new DateOnly(2026, 3, 8), 30.00m, "ZZLEK")));

        Assert.Equal(
            ["income", "housing", "food", "uncategorized-out", "uncategorized-in", "health"],
            view.Categories.Select(c => c.Category.Value));
    }

    [Fact]
    public void Categories_of_the_same_size_come_in_the_order_the_other_screens_use()
    {
        var withOwn = CategorizationConfig.Default.AddCategory("Vlastná", CategoryRole.Spending).Value!;
        var own = withOwn.Catalog.Categories[^1];
        var config = withOwn
            .AddUserRuleOnTop(new TextCondition("ZZVLASTNE", TextMode.Contains), new SubcategoryId($"{own.Id}.other")).Value!
            .AddUserRuleOnTop(new TextCondition("ZZJEDLO", TextMode.Contains), new SubcategoryId("food.groceries")).Value!;
        var view = new StatementViewModel(config);
        view.Show(Statement(
            Card(new DateOnly(2026, 3, 2), -100.00m, "NIEKDE"),
            Card(new DateOnly(2026, 3, 3), -100.00m, "ZZVLASTNE"),
            Card(new DateOnly(2026, 3, 4), -100.00m, "ZZJEDLO")));

        // The stored catalog has the user's own category after the uncategorized buckets; the screens show it before.
        Assert.Equal([new CategoryId("food"), own.Id, new CategoryId("uncategorized-out")], view.Categories.Select(c => c.Category));
    }

    [Fact]
    public void The_same_statement_shown_twice_is_in_the_same_order()
    {
        var first = Show("02-busy-month");
        var second = Show("02-busy-month");

        Assert.Equal(first.Categories, second.Categories);
        foreach (var row in first.Categories)
        {
            first.SelectedCategory = second.SelectedCategory = row.Category;
            Assert.Equal(first.Transactions.Select(t => t.Index), second.Transactions.Select(t => t.Index));
        }
    }

    [Fact]
    public void After_a_hand_choice_and_its_undo_the_cards_reorder_and_the_chosen_category_stays_chosen()
    {
        var statement = Statement(
            Card(new DateOnly(2026, 3, 3), -412.00m, "ZZJEDLO"),
            Card(new DateOnly(2026, 3, 6), -732.40m, "ZZBYT"),
            Card(new DateOnly(2026, 3, 9), -500.00m, "NIEKDE"));
        var key = FingerprintKey.Create();
        var config = RuleFor("ZZBYT", "housing.rent", "ZZJEDLO", "food.groceries").WithFingerprintKey(key);
        var view = new StatementViewModel(config);
        view.Show(statement);
        var food = new CategoryId("food");
        view.SelectedCategory = food;
        Assert.Equal(["housing", "uncategorized-out", "food"], view.Categories.Select(c => c.Category.Value));

        var fingerprint = Fingerprints.Of(statement, key)[2];
        view.Config = config.SetHandChoice(fingerprint, new SubcategoryId("food.restaurants")).Value!;

        Assert.Equal(["food", "housing"], view.Categories.Select(c => c.Category.Value));
        Assert.Equal(food, view.SelectedCategory);
        Assert.Equal(food, view.SelectedRow?.Category);
        Assert.Equal([-500.00m, -412.00m], view.Transactions.Select(t => t.Transaction.Amount));

        view.Config = view.Config.RemoveHandChoice(fingerprint);

        Assert.Equal(["housing", "uncategorized-out", "food"], view.Categories.Select(c => c.Category.Value));
        Assert.Equal(food, view.SelectedCategory);
        Assert.Equal([-412.00m], view.Transactions.Select(t => t.Transaction.Amount));
    }

    [Fact]
    public void After_a_rule_the_cards_reorder_and_a_chosen_category_left_empty_is_no_longer_chosen()
    {
        var statement = Statement(
            Card(new DateOnly(2026, 3, 3), -412.00m, "ZZJEDLO"),
            Card(new DateOnly(2026, 3, 6), -732.40m, "ZZBYT"),
            Card(new DateOnly(2026, 3, 9), -500.00m, "NIEKDE"));
        var view = new StatementViewModel(RuleFor("ZZBYT", "housing.rent"));
        view.Show(statement);
        view.SelectedCategory = new CategoryId("uncategorized-out");

        view.Config = view.Config.AddUserRuleOnTop(new TextCondition("ZZJEDLO", TextMode.Contains), new SubcategoryId("housing.rent")).Value!;

        Assert.Equal(["housing", "uncategorized-out"], view.Categories.Select(c => c.Category.Value));
        Assert.Equal(new CategoryId("uncategorized-out"), view.SelectedCategory);

        view.Config = view.Config.AddUserRuleOnTop(new TextCondition("NIEKDE", TextMode.Contains), new SubcategoryId("housing.rent")).Value!;

        Assert.Equal(["housing"], view.Categories.Select(c => c.Category.Value));
        Assert.Null(view.SelectedCategory);
        Assert.Empty(view.Transactions);
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void The_categorization_keeps_the_catalogs_own_order(string sample)
    {
        var view = Show(sample);

        Assert.Equal(view.Config.Catalog.Categories.Select(c => c.Id), view.Categorized!.Categories.Select(c => c.Category.Id));
    }

    /// <summary>The default configuration with a user rule for each text and target pair, as "contains".</summary>
    private static CategorizationConfig RuleFor(params string[] pairs)
    {
        var config = CategorizationConfig.Default;
        for (var i = 0; i < pairs.Length; i += 2)
            config = config.AddUserRuleOnTop(new TextCondition(pairs[i], TextMode.Contains), new SubcategoryId(pairs[i + 1])).Value!;
        return config;
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
    public void Every_row_carries_its_transactions_details_and_none_is_open_at_first()
    {
        var view = Show("02-busy-month");

        foreach (var row in view.Categories)
        {
            view.SelectedCategory = row.Category;
            Assert.Null(view.SelectedTransaction);
            Assert.All(view.Transactions, t => Assert.Equal(TransactionDetails.Of(t.Transaction), t.Details));
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
    public void A_row_of_the_charts_legend_selects_its_category()
    {
        var view = Show("01-typical-month");
        var slice = view.Slices[2];

        view.SelectCategoryCommand.Execute(slice.Category);

        Assert.Equal(slice.Category, view.SelectedCategory);
        Assert.Equal(slice.Category, view.SelectedRow?.Category);
    }

    [Fact]
    public void Money_coming_in_is_marked_so_it_can_be_shown_in_the_positive_colour()
    {
        var view = Show("01-typical-month");

        Assert.All(view.Categories, row => Assert.Equal(row.Amount > 0, row.IsIncoming));
        Assert.Contains(view.Categories, row => row.IsIncoming);
        foreach (var row in view.Categories)
        {
            view.SelectedCategory = row.Category;
            Assert.All(view.Transactions, t => Assert.Equal(t.Transaction.Amount > 0, t.IsIncoming));
        }
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
