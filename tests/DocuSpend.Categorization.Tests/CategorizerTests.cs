using System.Text.Json;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Results;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;
using static DocuSpend.Categorization.Tests.TestTransactions;

namespace DocuSpend.Categorization.Tests;

/// <summary>Categorizing a statement (spec: Deciding a transaction's category; What counts, and totals).</summary>
public class CategorizerTests
{
    private static readonly CategorizationConfig Config = CategorizationConfig.Default;

    private static Statement StatementOf(decimal opening, params Transaction[] transactions)
    {
        var regular = transactions.Where(t => t.FeeAmount is null).ToList();
        var deposits = regular.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var withdrawals = regular.Where(t => t.Amount < 0).Sum(t => t.Amount);
        var fees = -transactions.Where(t => t.FeeAmount is not null).Sum(t => t.Amount);
        return new Statement
        {
            AccountHolder = "TEST",
            Iban = "SK00",
            Bic = "TESTSKXX",
            Currency = "EUR",
            PeriodStart = new DateOnly(2026, 3, 1),
            PeriodEnd = new DateOnly(2026, 3, 31),
            OpeningBalance = opening,
            ClosingBalance = opening + deposits + withdrawals - fees,
            TotalDeposits = deposits,
            TotalWithdrawals = withdrawals,
            TotalFees = fees,
            Transactions = transactions,
        };
    }

    private static CategorizedStatement Run(CategorizationConfig config, params Transaction[] transactions) =>
        Categorizer.Categorize(StatementOf(1000m, transactions), config);

    private static CategorizedStatement Run(params Transaction[] transactions) => Run(Config, transactions);

    private static string Where(CategorizedTransaction t) => t.Placement.Subcategory?.Value ?? t.Placement.Category.Value;

    // Placement

    [Fact]
    public void Every_transaction_is_placed_in_order_and_counted_once()
    {
        var result = Run(Card("TESCO NITRA"), Card("NETFLIX.COM"), Card("MASIARSTVO"), Card("TESCO NITRA"));

        Assert.Equal(["food.groceries", "online-shopping.web-services", "uncategorized-out", "food.groceries"], result.Transactions.Select(Where));
        Assert.Equal(4, result.Transactions.Count);
    }

    [Fact]
    public void A_default_rule_decides_and_the_result_says_which()
    {
        var decision = Run(Card("NETFLIX.COM")).Transactions[0].Rule!;

        Assert.Equal(RuleSource.Default, decision.Source);
        Assert.Equal("default/online-shopping.web-services/netflix", decision.Id.Value);
    }

    [Fact]
    public void A_user_rule_decides_over_a_default_rule_and_the_result_says_which()
    {
        var config = Config.AddUserRule(new TextCondition("netflix", TextMode.Contains), new SubcategoryId("leisure.other"), 1).Value!;

        var transaction = Run(config, Card("NETFLIX.COM")).Transactions[0];

        Assert.Equal("leisure.other", Where(transaction));
        Assert.Equal(RuleSource.User, transaction.Rule!.Source);
        Assert.Equal(config.UserRules[0].Id, transaction.Rule.Id);
    }

    [Fact]
    public void A_transaction_no_rule_matches_goes_to_the_bucket_of_its_direction_with_no_rule()
    {
        var result = Run(Card("MASIARSTVO", amount: -12m), Transfer(amount: 30m, name: "Ján Príkladný"));

        Assert.Equal(["uncategorized-out", "uncategorized-in"], result.Transactions.Select(Where));
        Assert.All(result.Transactions, t => Assert.Null(t.Rule));
        Assert.All(result.Transactions, t => Assert.Null(t.Placement.Subcategory));
    }

    [Fact]
    public void A_zero_amount_counts_as_money_coming_in() =>
        Assert.Equal("uncategorized-in", Where(Run(Card("MASIARSTVO", amount: 0m)).Transactions[0]));

    [Fact]
    public void Any_rule_can_target_any_sub_category()
    {
        var config = Config.AddUserRule(new TextCondition("vratenie", TextMode.Contains), new SubcategoryId("food.groceries"), 1).Value!;

        Assert.Equal("food.groceries", Where(Run(config, Transfer(amount: 20m, purpose: "Vratenie tovaru")).Transactions[0]));
    }

    [Fact]
    public void Identical_transactions_are_categorized_independently_and_both_counted()
    {
        var result = Run(Card("TESCO", -5m), Card("TESCO", -5m));
        var groceries = result.Categories.Single(c => c.Category.Id.Value == "food");

        Assert.Equal(2, groceries.Total.Count);
        Assert.Equal(-10m, groceries.Total.Amount);
    }

    // What counts as spending

    [Fact]
    public void Spending_categories_count_as_spending_and_income_and_excluded_do_not()
    {
        var excluded = Config.AddUserRule(new CounterpartyAccountCondition("SK31 0200"), new SubcategoryId("own-transfers.other"), 1).Value!;
        var result = Run(excluded, Card("TESCO", -10m), Transfer(amount: 1500m, purpose: "MZDA"), Transfer(amount: -300m, account: "SK31 0200"));

        Assert.Equal([true, false, false], result.Transactions.Select(t => t.CountsAsSpending));
    }

    [Fact]
    public void An_uncategorized_expense_counts_as_spending_and_uncategorized_income_does_not()
    {
        var result = Run(Card("MASIARSTVO", -12m), Transfer(amount: 30m));

        Assert.Equal([true, false], result.Transactions.Select(t => t.CountsAsSpending));
    }

    [Fact]
    public void Totals_are_split_into_spending_income_and_excluded()
    {
        var config = Config
            .AddUserRule(new CounterpartyAccountCondition("SK31 0200"), new SubcategoryId("own-transfers.other"), 1).Value!
            .AddUserRule(new TextCondition("burza", TextMode.Contains), new SubcategoryId("investments.other"), 2).Value!;

        var result = Run(
            config,
            Card("TESCO", -40m),
            Card("MASIARSTVO", -10m),
            Transfer(amount: 1500m, purpose: "MZDA 02/2026"),
            Transfer(amount: -300m, account: "SK31 0200"),
            Transfer(amount: -200m, purpose: "Burza"));

        Assert.Equal(50m, result.SpendingTotal);
        Assert.Equal(1500m, result.IncomeTotal);
        Assert.Equal(-500m, result.ExcludedTotal);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void A_refund_in_a_spending_category_reduces_its_spending()
    {
        var config = Config.AddUserRule(new TextCondition("vratenie", TextMode.Contains), new SubcategoryId("food.groceries"), 1).Value!;

        var result = Run(config, Card("TESCO", -50m), Transfer(amount: 20m, purpose: "Vratenie tovaru"));
        var food = result.Categories.Single(c => c.Category.Id.Value == "food");

        Assert.Equal(30m, food.Spending);
        Assert.Equal(30m, result.SpendingTotal);
        Assert.Equal(0m, result.IncomeTotal);
    }

    [Fact]
    public void A_fee_and_its_reversal_cancel_out_to_a_spending_of_zero()
    {
        var result = Run(Fee(-6.90m), Fee(6.90m));
        var fees = result.Categories.Single(c => c.Category.Id.Value == "fees");

        Assert.Equal(["fees.fees", "fees.fees"], result.Transactions.Select(Where));
        Assert.Equal(0m, fees.Spending);
        Assert.Equal(2, fees.Total.Count);
        Assert.Equal(0m, result.SpendingTotal);
    }

    [Fact]
    public void Spending_can_be_negative_when_more_came_back_than_went_out()
    {
        var config = Config.AddUserRule(new TextCondition("vratenie", TextMode.Contains), new SubcategoryId("food.groceries"), 1).Value!;

        var result = Run(config, Card("TESCO", -10m), Transfer(amount: 25m, purpose: "Vratenie"));

        Assert.Equal(-15m, result.Categories.Single(c => c.Category.Id.Value == "food").Spending);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void A_user_category_with_the_excluded_role_is_left_out_of_spending()
    {
        var withCategory = Config.AddCategory("Pôžičky", CategoryRole.Excluded).Value!;
        var target = withCategory.Catalog.Categories.Single(c => c.Name == "Pôžičky").Subcategories.Single().Id;
        var config = withCategory.AddUserRule(new TextCondition("pozicka", TextMode.Contains), target, 1).Value!;

        var result = Run(config, Transfer(amount: -100m, purpose: "Pozicka kamosovi"), Card("TESCO", -5m));

        Assert.Equal(5m, result.SpendingTotal);
        Assert.Equal(-100m, result.ExcludedTotal);
    }

    // Summaries

    [Fact]
    public void Every_category_and_sub_category_is_in_the_summary_even_when_empty()
    {
        var result = Run(Card("TESCO", -5m));

        Assert.Equal(Config.Catalog.Categories.Select(c => c.Id), result.Categories.Select(c => c.Category.Id));
        Assert.Equal(
            Config.Catalog.Categories.SelectMany(c => c.Subcategories).Select(s => s.Id),
            result.Categories.SelectMany(c => c.Subcategories).Select(s => s.Subcategory.Id));
        Assert.Equal(0, result.Categories.Single(c => c.Category.Id.Value == "housing").Total.Count);
    }

    [Fact]
    public void A_categorys_balance_is_the_sum_of_its_sub_categories()
    {
        var result = Run(Card("TESCO", -5m), Card("PEKAREN", -3m), Card("MCDONALD", -7m), Card("NETFLIX", -12m));

        foreach (var category in result.Categories.Where(c => !c.Category.IsUncategorized))
        {
            Assert.Equal(category.Subcategories.Sum(s => s.Total.Amount), category.Total.Amount);
            Assert.Equal(category.Subcategories.Sum(s => s.Total.Count), category.Total.Count);
        }
        Assert.Equal(-15m, result.Categories.Single(c => c.Category.Id.Value == "food").Total.Amount);
    }

    [Fact]
    public void Spending_is_only_reported_for_spending_categories()
    {
        var result = Run(Card("TESCO", -5m), Transfer(amount: 100m, purpose: "MZDA"));

        Assert.Equal(5m, result.Categories.Single(c => c.Category.Id.Value == "food").Spending);
        Assert.Null(result.Categories.Single(c => c.Category.Id.Value == "income").Spending);
        Assert.Null(result.Categories.Single(c => c.Category.Id.Value == "investments").Spending);
    }

    [Fact]
    public void An_empty_statement_categorizes_to_nothing_with_all_totals_zero()
    {
        var result = Run();

        Assert.Empty(result.Transactions);
        Assert.Equal((0m, 0m, 0m), (result.SpendingTotal, result.IncomeTotal, result.ExcludedTotal));
        Assert.All(result.Categories, c => Assert.Equal(0, c.Total.Count));
        Assert.True(result.IsComplete);
    }

    // Reproducible and harmless

    [Fact]
    public void The_same_statement_and_rules_always_give_the_same_result()
    {
        var statement = StatementOf(1000m, Card("TESCO", -5m), Transfer(amount: 90m, purpose: "MZDA"), Card("MASIARSTVO", -3m));

        var first = Categorizer.Categorize(statement, Config);
        var second = Categorizer.Categorize(statement, Config);

        Assert.Equal(first.Transactions.Select(Where), second.Transactions.Select(Where));
        Assert.Equal(first.Transactions.Select(t => t.Rule?.Id), second.Transactions.Select(t => t.Rule?.Id));
        Assert.Equal((first.SpendingTotal, first.IncomeTotal, first.ExcludedTotal), (second.SpendingTotal, second.IncomeTotal, second.ExcludedTotal));
    }

    [Fact]
    public void The_statement_is_not_changed()
    {
        var statement = StatementOf(1000m, Card("TESCO", -5m), Transfer(amount: 90m, purpose: "MZDA"));
        var before = JsonSerializer.Serialize(statement);

        var result = Categorizer.Categorize(statement, Config);

        Assert.Equal(before, JsonSerializer.Serialize(statement));
        Assert.Same(statement, result.Statement);
    }

    [Fact]
    public void The_transactions_in_the_result_are_the_statements_own()
    {
        var statement = StatementOf(1000m, Card("TESCO", -5m));

        var result = Categorizer.Categorize(statement, Config);

        Assert.Same(statement.Transactions[0], result.Transactions[0].Transaction);
    }

    [Fact]
    public void Completeness_is_false_when_the_statements_own_balances_do_not_add_up()
    {
        var statement = StatementOf(1000m, Card("TESCO", -5m)) with { ClosingBalance = 1.00m };

        Assert.False(Categorizer.Categorize(statement, Config).IsComplete);
    }
}
