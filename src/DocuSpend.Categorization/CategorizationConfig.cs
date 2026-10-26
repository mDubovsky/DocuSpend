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
    /// <summary>
    /// The highest number a change may give a user rule. The screen shows built-in rules as one more, since every
    /// user rule wins over them.
    /// </summary>
    public const int MaxUserPriority = 255;

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
        if (Check(condition, target, priority) is { } refusal)
            return Outcome<CategorizationConfig>.Refused(refusal);

        var order = UserRules.Count == 0 ? 1 : UserRules.Max(r => r.Order) + 1;
        var rule = new Rule(new RuleId($"user-{Guid.NewGuid():N}"), RuleSource.User, priority, order, condition, target);
        return Outcome<CategorizationConfig>.Ok(new CategorizationConfig(Catalog, DefaultRules, [.. UserRules, rule]));
    }

    /// <summary>
    /// Adds a user rule that beats every existing user rule without the caller choosing a number: it gets the lowest
    /// number any user rule has (1 if there are none; at most <see cref="MaxUserPriority"/>, in case a rule saved
    /// before that bound has a higher one) and, being the newest, wins the tie. This is how correcting a wrongly
    /// categorized transaction becomes just adding a rule.
    /// </summary>
    public Outcome<CategorizationConfig> AddUserRuleOnTop(Condition condition, SubcategoryId target) =>
        AddUserRule(condition, target, UserRules.Count == 0 ? 1 : Math.Min(UserRules.Min(r => r.Priority), MaxUserPriority));

    /// <summary>Changes a user rule's priority number. Its creation order, and so its place among equals, stays.</summary>
    public Outcome<CategorizationConfig> SetUserRulePriority(RuleId id, int priority)
    {
        if (UserRules.All(r => r.Id != id))
            return Outcome<CategorizationConfig>.Refused(UnknownRule);
        if (CheckPriority(priority) is { } refusal)
            return Outcome<CategorizationConfig>.Refused(refusal);

        return Outcome<CategorizationConfig>.Ok(Replaced(id, r => r with { Priority = priority }));
    }

    /// <summary>
    /// Changes a user rule's condition, target and priority, checked as when adding. It stays the same rule: its
    /// identifier and creation order, and so its place among rules with the same number, don't change.
    /// </summary>
    public Outcome<CategorizationConfig> ChangeUserRule(RuleId id, Condition condition, SubcategoryId target, int priority)
    {
        if (UserRules.All(r => r.Id != id))
            return Outcome<CategorizationConfig>.Refused(UnknownRule);
        if (Check(condition, target, priority) is { } refusal)
            return Outcome<CategorizationConfig>.Refused(refusal);

        return Outcome<CategorizationConfig>.Ok(Replaced(id, r => r with { Condition = condition, Target = target, Priority = priority }));
    }

    /// <summary>Removes a user rule. Default rules can't be removed: only user rules have these identifiers.</summary>
    public Outcome<CategorizationConfig> RemoveUserRule(RuleId id)
    {
        if (UserRules.All(r => r.Id != id))
            return Outcome<CategorizationConfig>.Refused(UnknownRule);

        return Outcome<CategorizationConfig>.Ok(new CategorizationConfig(Catalog, DefaultRules, UserRules.Where(r => r.Id != id).ToList()));
    }

    private static Refusal UnknownRule => new(RefusalKind.UnknownRule, "There is no user rule with this identifier.");

    private CategorizationConfig Replaced(RuleId id, Func<Rule, Rule> change) =>
        new(Catalog, DefaultRules, UserRules.Select(r => r.Id == id ? change(r) : r).ToList());

    private Refusal? Check(Condition condition, SubcategoryId target, int priority)
    {
        if (condition.Validate() is { } invalid)
            return invalid;
        if (CheckPriority(priority) is { } refusal)
            return refusal;
        if (Catalog.FindSubcategory(target) is null)
            return new Refusal(RefusalKind.UnknownTarget, "The rule's target is not an existing sub-category.");
        return null;
    }

    /// <summary>
    /// A change may give a user rule a number from 1 to <see cref="MaxUserPriority"/>. Rules saved before the upper
    /// bound existed may have a higher number; they load and work as before.
    /// </summary>
    private static Refusal? CheckPriority(int priority) =>
        priority is >= 1 and <= MaxUserPriority
            ? null
            : new Refusal(RefusalKind.InvalidPriority, $"A priority number is a whole number from 1 to {MaxUserPriority}.");

    private Outcome<CategorizationConfig> With(Outcome<CategoryCatalog> catalog) =>
        catalog.IsSuccess
            ? Outcome<CategorizationConfig>.Ok(new CategorizationConfig(catalog.Value!, DefaultRules, UserRules))
            : Outcome<CategorizationConfig>.Refused(catalog.Refusal!);
}
