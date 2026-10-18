using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Results;

/// <summary>Which rule put a transaction where it is.</summary>
public sealed record RuleReference(RuleId Id, RuleSource Source);

/// <summary>A transaction with its category.</summary>
/// <param name="Placement">A sub-category, or for the two Uncategorized buckets just the category.</param>
/// <param name="Rule">The rule that decided, or <c>null</c> when no rule matched and the transaction went to an Uncategorized bucket.</param>
/// <param name="CountsAsSpending">Whether the category's role is spending. Money coming back into a spending category counts too, as a reduction.</param>
public sealed record CategorizedTransaction(Transaction Transaction, Placement Placement, RuleReference? Rule, bool CountsAsSpending);

/// <summary>How many transactions, and what they add up to: the sum of their signed amounts (negative for money going out).</summary>
public sealed record Balance(int Count, decimal Amount);

public sealed record SubcategorySummary(Subcategory Subcategory, Balance Total);

/// <summary>A category's balance is the sum of its sub-categories' balances; an Uncategorized bucket has its own.</summary>
public sealed record CategorySummary(Category Category, Balance Total, IReadOnlyList<SubcategorySummary> Subcategories)
{
    /// <summary>
    /// What was spent in a spending category: the negative of its balance, so money that came back (a refund, a reversed
    /// fee) reduces it, and negative when more came back than went out. <c>null</c> for income and excluded categories.
    /// </summary>
    public decimal? Spending => Category.Role == CategoryRole.Spending ? -Total.Amount : null;
}

/// <summary>A statement with every transaction in a category, and the totals.</summary>
/// <param name="SpendingTotal">The sum of the spending of all spending categories.</param>
/// <param name="IncomeTotal">The sum of the balances of income categories.</param>
/// <param name="ExcludedTotal">The sum of the balances of excluded categories.</param>
public sealed record CategorizedStatement(
    Statement Statement,
    IReadOnlyList<CategorizedTransaction> Transactions,
    IReadOnlyList<CategorySummary> Categories,
    decimal SpendingTotal,
    decimal IncomeTotal,
    decimal ExcludedTotal)
{
    /// <summary>
    /// Nothing was lost: the closing balance minus the opening balance equals income plus excluded minus the spending
    /// total. False means the statement's own balances don't add up, since every transaction is in exactly one category.
    /// </summary>
    public bool IsComplete =>
        Statement.ClosingBalance - Statement.OpeningBalance == IncomeTotal + ExcludedTotal - SpendingTotal;
}
