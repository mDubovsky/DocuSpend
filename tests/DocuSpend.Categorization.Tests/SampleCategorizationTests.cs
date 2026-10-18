using DocuSpend.Categorization.Results;

namespace DocuSpend.Categorization.Tests;

/// <summary>Categorizing the committed sample statements, read with the real reader (spec: Acceptance criteria).</summary>
public class SampleCategorizationTests
{
    private static readonly CategorizationConfig Config = CategorizationConfig.Default;

    public static TheoryData<string> Names => new(Samples.Valid);

    private static CategorizedStatement Categorize(string name) => Categorizer.Categorize(Samples.Read(name), Config);

    private static string Where(CategorizedTransaction t) => t.Placement.Subcategory?.Value ?? t.Placement.Category.Value;

    [Theory]
    [MemberData(nameof(Names))]
    public void Every_transaction_is_in_exactly_one_sub_category_or_bucket(string name)
    {
        var statement = Samples.Read(name);
        var result = Categorizer.Categorize(statement, Config);

        Assert.Equal(statement.Transactions.Count, result.Transactions.Count);
        Assert.All(result.Transactions, t =>
        {
            var (category, subcategory) = Config.Catalog.Describe(t.Placement);
            Assert.Equal(category.IsUncategorized, subcategory is null);
        });
        Assert.Equal(statement.Transactions.Count, result.Categories.Sum(c => c.Total.Count));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Nothing_is_lost_closing_minus_opening_equals_income_plus_excluded_minus_spending(string name)
    {
        var result = Categorize(name);
        var statement = result.Statement;

        Assert.True(result.IsComplete);
        Assert.Equal(statement.ClosingBalance - statement.OpeningBalance, result.IncomeTotal + result.ExcludedTotal - result.SpendingTotal);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void A_categorys_balance_equals_the_sum_of_its_sub_categories(string name)
    {
        foreach (var category in Categorize(name).Categories.Where(c => !c.Category.IsUncategorized))
        {
            Assert.Equal(category.Subcategories.Sum(s => s.Total.Amount), category.Total.Amount);
            Assert.Equal(category.Subcategories.Sum(s => s.Total.Count), category.Total.Count);
        }
    }

    [Fact]
    public void Sample_one_puts_the_known_examples_where_the_spec_says()
    {
        var result = Categorize("01-typical-month");
        string Of(Func<DocuSpend.Statements.Model.Transaction, bool> match) => Where(result.Transactions.Single(t => match(t.Transaction)));

        Assert.Equal("online-shopping.web-services", Of(t => t.Description == "NETFLIX.COM"));
        Assert.Equal("housing.rent", Of(t => t.PaymentPurpose == "Najomne marec"));
        Assert.Equal("income.salary", Of(t => t.PaymentPurpose == "MZDA 02/2026"));
    }

    [Fact]
    public void Sample_one_has_the_fee_and_its_reversal_in_fees_cancelling_to_zero()
    {
        var result = Categorize("01-typical-month");
        var fees = result.Categories.Single(c => c.Category.Id.Value == "fees");

        Assert.Equal(2, fees.Total.Count);
        Assert.Equal(0m, fees.Spending);
        Assert.Equal(2, fees.Subcategories.Single(s => s.Subcategory.Id.Value == "fees.fees").Total.Count);
    }

    [Fact]
    public void Sample_one_salary_is_income_and_not_counted_as_spending()
    {
        var result = Categorize("01-typical-month");
        var salary = result.Transactions.Single(t => t.Transaction.PaymentPurpose == "MZDA 02/2026");

        Assert.False(salary.CountsAsSpending);
        // Income is the incoming payments outside spending categories: the salary, a gift and an instant payment.
        // The fee reversal is money in too, but it sits in Poplatky a úroky, where it cancels the fee.
        Assert.Equal(
            result.Transactions.Where(t => !t.CountsAsSpending && t.Transaction.Amount > 0).Sum(t => t.Transaction.Amount),
            result.IncomeTotal);
        Assert.Equal(-result.Transactions.Where(t => t.CountsAsSpending).Sum(t => t.Transaction.Amount), result.SpendingTotal);
    }

    [Fact]
    public void Sample_one_categorizes_most_of_its_everyday_payments_by_default_rules()
    {
        var result = Categorize("01-typical-month");

        // The sample has a few merchants no default rule knows (a butcher, a garden centre, an optician's neighbour)
        // and instant payments to people, which no default can know.
        var decidedByRules = result.Transactions.Count(t => t.Rule is not null);
        Assert.True(decidedByRules >= 15, $"{decidedByRules} of {result.Transactions.Count}");
        Assert.Contains(result.Transactions, t => t.Rule is null);
    }

    [Fact]
    public void The_empty_month_categorizes_to_nothing()
    {
        var result = Categorize("04-empty-month");

        Assert.Empty(result.Transactions);
        Assert.Equal(0m, result.SpendingTotal);
        Assert.Equal(0m, result.IncomeTotal);
        Assert.Equal(0m, result.ExcludedTotal);
    }

    [Fact]
    public void The_single_transaction_month_has_exactly_one_placement()
    {
        var result = Categorize("05-single-transaction-month");

        Assert.Single(result.Transactions);
        Assert.Equal(1, result.Categories.Sum(c => c.Total.Count));
    }

    [Fact]
    public void Sample_three_with_its_awkward_text_is_categorized_without_trouble()
    {
        var result = Categorize("03-awkward-month");

        // The pharmacy is "LEKÁREŇ ŠŤASTNÁ RUKA": diacritics don't stop the default rule.
        var pharmacy = result.Transactions.Single(t => t.Transaction.Description?.StartsWith("LEKÁREŇ", StringComparison.Ordinal) == true);
        Assert.Equal("health.other", Where(pharmacy));
    }
}
