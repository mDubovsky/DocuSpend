using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Results;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization;

/// <summary>
/// Puts every transaction of a statement in a category (spec: Deciding a transaction's category). A pure function:
/// it never changes the statement, never fails, uses no clock or randomness, and gives the same result for the same
/// statement and configuration every time.
/// </summary>
public static class Categorizer
{
    public static CategorizedStatement Categorize(Statement statement, CategorizationConfig config)
    {
        var catalog = config.Catalog;
        var rules = config.RulesInOrder().ToList();
        var categorized = new List<CategorizedTransaction>(statement.Transactions.Count);

        foreach (var transaction in statement.Transactions)
        {
            var rule = rules.FirstOrDefault(r => r.Condition.Matches(transaction));
            var (category, placement) = Place(catalog, rule, transaction);
            categorized.Add(new CategorizedTransaction(
                transaction,
                placement,
                rule is null ? null : new RuleReference(rule.Id, rule.Source),
                category.Role == CategoryRole.Spending));
        }

        var summaries = Summarize(catalog, categorized);
        return new CategorizedStatement(
            statement,
            categorized,
            summaries,
            SpendingTotal: -Sum(summaries, CategoryRole.Spending),
            IncomeTotal: Sum(summaries, CategoryRole.Income),
            ExcludedTotal: Sum(summaries, CategoryRole.Excluded));
    }

    /// <summary>The matching rule's sub-category, or the Uncategorized bucket of the transaction's direction.</summary>
    private static (Category Category, Placement Placement) Place(CategoryCatalog catalog, Rules.Rule? rule, Transaction transaction)
    {
        if (rule is not null && catalog.FindSubcategory(rule.Target) is { } found)
            return (found.Category, new Placement(found.Category.Id, found.Subcategory.Id));

        var bucket = catalog.UncategorizedFor(incoming: transaction.Amount >= 0);
        return (bucket, new Placement(bucket.Id, null));
    }

    private static List<CategorySummary> Summarize(CategoryCatalog catalog, List<CategorizedTransaction> transactions)
    {
        var bySub = transactions
            .Where(t => t.Placement.Subcategory is not null)
            .GroupBy(t => t.Placement.Subcategory!.Value)
            .ToDictionary(g => g.Key, g => Total(g));
        var byBucket = transactions
            .Where(t => t.Placement.Subcategory is null)
            .GroupBy(t => t.Placement.Category)
            .ToDictionary(g => g.Key, g => Total(g));

        return catalog.Categories.Select(category =>
        {
            var subs = category.Subcategories
                .Select(s => new SubcategorySummary(s, bySub.GetValueOrDefault(s.Id, new Balance(0, 0m))))
                .ToList();
            var total = category.IsUncategorized
                ? byBucket.GetValueOrDefault(category.Id, new Balance(0, 0m))
                : new Balance(subs.Sum(s => s.Total.Count), subs.Sum(s => s.Total.Amount));
            return new CategorySummary(category, total, subs);
        }).ToList();
    }

    private static Balance Total(IEnumerable<CategorizedTransaction> transactions)
    {
        var list = transactions.ToList();
        return new Balance(list.Count, list.Sum(t => t.Transaction.Amount));
    }

    private static decimal Sum(IEnumerable<CategorySummary> summaries, CategoryRole role) =>
        summaries.Where(s => s.Category.Role == role).Sum(s => s.Total.Amount);
}
