using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Results;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization;

/// <summary>
/// Puts every transaction of a statement in a category (spec: Deciding a transaction's category). A pure function:
/// it never changes the statement, never fails, uses no clock or randomness, and gives the same result for the same
/// statement and configuration every time.
/// </summary>
public static class Categorizer
{
    /// <summary>
    /// Default rules never place a cash withdrawal: its description names the cash machine, often inside a shop, so a
    /// text rule would call the cash shopping. It stays uncategorized until the user decides
    /// (work/041-unknown-transaction-kinds/spec.md).
    /// </summary>
    private static bool Applies(Rule rule, Transaction transaction) =>
        rule.Source == RuleSource.User || transaction.Type != TransactionType.CashWithdrawal;

    public static CategorizedStatement Categorize(Statement statement, CategorizationConfig config)
    {
        var catalog = config.Catalog;
        var rules = config.RulesInOrder().ToList();
        var categorized = new List<CategorizedTransaction>(statement.Transactions.Count);
        var fingerprints = config.FingerprintKey is { } key ? Fingerprints.Of(statement, key) : null;
        var choices = config.HandChoices.ToDictionary(c => c.Fingerprint, c => c.Target);

        for (var i = 0; i < statement.Transactions.Count; i++)
        {
            var transaction = statement.Transactions[i];
            var fingerprint = fingerprints?[i];

            // A hand choice wins over every rule (work/022-categorize-from-list/spec.md: "Len túto transakciu").
            if (fingerprint is { } print && choices.TryGetValue(print, out var target) && catalog.FindSubcategory(target) is { } chosen)
            {
                categorized.Add(new CategorizedTransaction(
                    transaction,
                    new Placement(chosen.Category.Id, chosen.Subcategory.Id),
                    Rule: null,
                    chosen.Category.Role == CategoryRole.Spending,
                    ByHand: true,
                    fingerprint));
                continue;
            }

            var rule = rules.FirstOrDefault(r => Applies(r, transaction) && r.Condition.Matches(transaction));
            var (category, placement) = Place(catalog, rule, transaction);
            categorized.Add(new CategorizedTransaction(
                transaction,
                placement,
                rule is null ? null : new RuleReference(rule.Id, rule.Source),
                category.Role == CategoryRole.Spending,
                ByHand: false,
                fingerprint));
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
