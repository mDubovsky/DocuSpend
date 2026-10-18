using DocuSpend.Categorization.BuiltIn;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;

namespace DocuSpend.Categorization;

/// <summary>
/// Everything that decides a category: the categories, the default rules and the user rules. Immutable: every change
/// returns a new configuration or a refusal, so the invariants below can't be broken from outside.
/// <list type="bullet">
/// <item>every rule's target is an existing sub-category;</item>
/// <item>every priority number is at least 1, and default numbers are unique;</item>
/// <item>every user rule has a creation order, unique and increasing; for equal numbers the newest rule wins;</item>
/// <item>the category invariants of <see cref="CategoryCatalog"/>.</item>
/// </list>
/// The user-made part (<see cref="UserRules"/> and the user categories in <see cref="Catalog"/>) is plain data, so
/// saving it later is straightforward.
/// </summary>
public sealed class CategorizationConfig
{
    private CategorizationConfig(CategoryCatalog catalog, IReadOnlyList<Rule> defaultRules, IReadOnlyList<Rule> userRules)
    {
        Catalog = catalog;
        DefaultRules = defaultRules;
        UserRules = userRules;
    }

    /// <summary>The built-in categories and default rules, and no user rules.</summary>
    public static CategorizationConfig Default { get; } = Create(BuiltInCatalog.Create(), BuiltInRules.Create());

    public CategoryCatalog Catalog { get; }

    /// <summary>In the order they are tried: by priority number.</summary>
    public IReadOnlyList<Rule> DefaultRules { get; }

    /// <summary>In the order they were created.</summary>
    public IReadOnlyList<Rule> UserRules { get; }

    /// <summary>
    /// Builds a configuration from its parts, checking them. Throws when the parts break an invariant, since for
    /// default rules that is a mistake in the program, not something a user did.
    /// </summary>
    internal static CategorizationConfig Create(CategoryCatalog catalog, IEnumerable<Rule> defaultRules, IEnumerable<Rule>? userRules = null)
    {
        var defaults = defaultRules.OrderBy(r => r.Priority).ToList();
        var users = (userRules ?? []).OrderBy(r => r.Order).ToList();

        foreach (var rule in defaults.Concat(users))
        {
            if (catalog.FindSubcategory(rule.Target) is null)
                throw new ArgumentException($"Rule {rule.Id} targets a sub-category that doesn't exist.");
            if (rule.Priority < 1)
                throw new ArgumentException($"Rule {rule.Id} has a priority below 1.");
        }
        if (defaults.Select(r => r.Priority).Distinct().Count() != defaults.Count)
            throw new ArgumentException("Default rules must have distinct priority numbers.");
        if (users.Select(r => r.Order).Distinct().Count() != users.Count)
            throw new ArgumentException("User rules must have distinct creation orders.");

        return new CategorizationConfig(catalog, defaults, users);
    }

    /// <summary>
    /// The rules in the order they are tried: user rules by priority number and, for equal numbers, newest first;
    /// then default rules by priority number. The first rule whose condition matches decides.
    /// </summary>
    public IEnumerable<Rule> RulesInOrder() =>
        UserRules.OrderBy(r => r.Priority).ThenByDescending(r => r.Order)
            .Concat(DefaultRules.OrderBy(r => r.Priority));

    public Outcome<CategorizationConfig> AddCategory(string name, CategoryRole role) =>
        With(Catalog.AddCategory(name, role));

    public Outcome<CategorizationConfig> AddSubcategory(CategoryId categoryId, string name) =>
        With(Catalog.AddSubcategory(categoryId, name));

    /// <summary>Adds a user rule with the given priority number. Another user rule may have the same number.</summary>
    public Outcome<CategorizationConfig> AddUserRule(Condition condition, SubcategoryId target, int priority)
    {
        if (condition.Validate() is { } invalid)
            return Outcome<CategorizationConfig>.Refused(invalid);
        if (priority < 1)
            return Outcome<CategorizationConfig>.Refused(RefusalKind.InvalidPriority, "A priority number is a whole number from 1.");
        if (Catalog.FindSubcategory(target) is null)
            return Outcome<CategorizationConfig>.Refused(RefusalKind.UnknownTarget, "The rule's target is not an existing sub-category.");

        var order = UserRules.Count == 0 ? 1 : UserRules.Max(r => r.Order) + 1;
        var rule = new Rule(new RuleId($"user-{Guid.NewGuid():N}"), RuleSource.User, priority, order, condition, target);
        return Outcome<CategorizationConfig>.Ok(new CategorizationConfig(Catalog, DefaultRules, [.. UserRules, rule]));
    }

    /// <summary>
    /// Adds a user rule that beats every existing user rule without the caller choosing a number: it gets the lowest
    /// number any user rule has (1 if there are none) and, being the newest, wins the tie. This is how correcting a
    /// wrongly categorized transaction becomes just adding a rule.
    /// </summary>
    public Outcome<CategorizationConfig> AddUserRuleOnTop(Condition condition, SubcategoryId target) =>
        AddUserRule(condition, target, UserRules.Count == 0 ? 1 : UserRules.Min(r => r.Priority));

    /// <summary>Changes a user rule's priority number. Its creation order, and so its place among equals, stays.</summary>
    public Outcome<CategorizationConfig> SetUserRulePriority(RuleId id, int priority)
    {
        if (UserRules.All(r => r.Id != id))
            return Outcome<CategorizationConfig>.Refused(RefusalKind.UnknownRule, "There is no user rule with this identifier.");
        if (priority < 1)
            return Outcome<CategorizationConfig>.Refused(RefusalKind.InvalidPriority, "A priority number is a whole number from 1.");

        var changed = UserRules.Select(r => r.Id == id ? r with { Priority = priority } : r).ToList();
        return Outcome<CategorizationConfig>.Ok(new CategorizationConfig(Catalog, DefaultRules, changed));
    }

    private Outcome<CategorizationConfig> With(Outcome<CategoryCatalog> catalog) =>
        catalog.IsSuccess
            ? Outcome<CategorizationConfig>.Ok(new CategorizationConfig(catalog.Value!, DefaultRules, UserRules))
            : Outcome<CategorizationConfig>.Refused(catalog.Refusal!);
}
