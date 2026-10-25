using System.Text.Json;
using DocuSpend.Categorization.Model;
using DocuSpend.Categorization.Rules;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Saving;

/// <summary>What reading saved rules found.</summary>
public enum SavedRulesStatus
{
    /// <summary>The saved rules were read; <see cref="SavedRulesReading.Config"/> holds them.</summary>
    Loaded,

    /// <summary>The text can't be understood as saved rules of any version.</summary>
    Damaged,

    /// <summary>The text was written by a newer DocuSpend: a later shape, or a built-in this version doesn't have.</summary>
    Newer,
}

/// <summary>
/// The result of <see cref="SavedRules.Read"/>. <see cref="Reason"/> says what was wrong, for tests and developers; it
/// is English, never shown to the user, and never contains anything from the text.
/// </summary>
public sealed record SavedRulesReading(SavedRulesStatus Status, CategorizationConfig? Config, string? Reason)
{
    internal static SavedRulesReading Damaged(string reason) => new(SavedRulesStatus.Damaged, null, reason);

    internal static SavedRulesReading Newer(string reason) => new(SavedRulesStatus.Newer, null, reason);
}

/// <summary>
/// The user's part of the categorization as text, and back (spec: what is kept). Only what the user made is written:
/// their categories, their sub-categories in built-in categories and their rules. The built-in categories and the
/// default rules come from the DocuSpend that reads the text. Nothing here touches files.
/// </summary>
public static class SavedRules
{
    /// <summary>The shape <see cref="Write"/> produces. A change to the shape raises it and adds a reader for the old one.</summary>
    public const int CurrentVersion = 1;

    private const string UserPrefix = "user-";

    public static string Write(CategorizationConfig config)
    {
        var categories = config.Catalog.Categories
            .Where(c => !c.IsBuiltIn)
            .Select(c => new SavedCategory(c.Id.Value, c.Name, Words.Role(c.Role),
                c.Subcategories.Select(s => new SavedSubcategory(s.Id.Value, s.Name)).ToList()))
            .ToList();
        var subcategories = config.Catalog.Categories
            .Where(c => c.IsBuiltIn)
            .SelectMany(c => c.Subcategories.Where(s => IsUser(s.Id.Value)).Select(s => new SavedSubcategoryIn(c.Id.Value, s.Id.Value, s.Name)))
            .ToList();
        var rules = config.UserRules
            .Select(r => new SavedRule(r.Id.Value, r.Priority, r.Order, r.Target.Value, Save(r.Condition)))
            .ToList();
        return JsonSerializer.Serialize(new SavedFile(CurrentVersion, categories, subcategories, rules), SavedRulesJson.Strict.SavedFile);
    }

    /// <summary>
    /// Reads saved rules on top of <paramref name="defaults"/>, whose categories and default rules are the built-in
    /// ones. Everything is checked before anything is used: one thing wrong makes the whole text damaged (spec:
    /// decisions), so a partial result never differs silently from what the user had.
    /// </summary>
    public static SavedRulesReading Read(string text, CategorizationConfig defaults)
    {
        int version;
        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("version", out var element)
                || element.ValueKind != JsonValueKind.Number
                || !element.TryGetInt32(out version))
                return SavedRulesReading.Damaged("The text has no whole-number version.");
        }
        catch (JsonException)
        {
            return SavedRulesReading.Damaged("The text is not JSON.");
        }

        if (version > CurrentVersion)
            return SavedRulesReading.Newer($"The text is of version {version}; this DocuSpend knows up to {CurrentVersion}.");
        if (version < 1)
            return SavedRulesReading.Damaged($"Version {version} never existed.");

        SavedFile? file;
        try
        {
            file = JsonSerializer.Deserialize(text, SavedRulesJson.Strict.SavedFile);
        }
        catch (JsonException e)
        {
            return SavedRulesReading.Damaged($"The text doesn't have the shape of version {version}: {e.Path}.");
        }
        return file is null ? SavedRulesReading.Damaged("The text is null.") : Restore(file, defaults);
    }

    private static SavedRulesReading Restore(SavedFile file, CategorizationConfig defaults)
    {
        var builtIn = defaults.Catalog;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        bool Fresh(string id) => id.Length > UserPrefix.Length && IsUser(id) && ids.Add(id);

        // The user's own categories, each with its Ostatné.
        var userCategories = new List<Category>();
        foreach (var saved in file.Categories)
        {
            if (!Fresh(saved.Id))
                return SavedRulesReading.Damaged("A user category has an empty, built-in-like or repeated identifier.");
            if (Words.ParseRole(saved.Role) is not { } role)
                return SavedRulesReading.Damaged("A user category has an unknown role.");
            if (IsBlank(saved.Name) || userCategories.Any(c => SameName(c.Name, saved.Name)))
                return SavedRulesReading.Damaged("A user category has a blank or repeated name.");
            var subs = new List<Subcategory>();
            foreach (var sub in saved.Subcategories)
            {
                if (sub.Id != $"{saved.Id}.other" && !Fresh(sub.Id))
                    return SavedRulesReading.Damaged("A sub-category has an empty, built-in-like or repeated identifier.");
                if (IsBlank(sub.Name) || subs.Any(s => SameName(s.Name, sub.Name)))
                    return SavedRulesReading.Damaged("A sub-category has a blank name or one its sibling has.");
                subs.Add(new Subcategory(new SubcategoryId(sub.Id), sub.Name));
            }
            if (subs.Count(s => s.Id.Value == $"{saved.Id}.other") != 1 || !ids.Add($"{saved.Id}.other"))
                return SavedRulesReading.Damaged("A user category doesn't have exactly one Ostatné sub-category.");
            userCategories.Add(new Category(new CategoryId(saved.Id), saved.Name, role, subs, IsBuiltIn: false));
        }

        // The user's sub-categories in built-in categories. A name may match a built-in sibling's: a newer DocuSpend
        // can add a built-in sub-category the user had already made for themselves, and both stay.
        var added = new Dictionary<CategoryId, List<Subcategory>>();
        foreach (var saved in file.Subcategories)
        {
            if (IsUser(saved.Category))
                return SavedRulesReading.Damaged("A sub-category is listed for a user category outside that category.");
            if (builtIn.FindCategory(new CategoryId(saved.Category)) is not { } category)
                return SavedRulesReading.Newer("A sub-category is in a built-in category this DocuSpend doesn't have.");
            if (category.IsUncategorized)
                return SavedRulesReading.Damaged("A sub-category is in an Uncategorized bucket.");
            if (!Fresh(saved.Id))
                return SavedRulesReading.Damaged("A sub-category has an empty, built-in-like or repeated identifier.");
            var siblings = added.TryGetValue(category.Id, out var list) ? list : added[category.Id] = [];
            if (IsBlank(saved.Name) || siblings.Any(s => SameName(s.Name, saved.Name)))
                return SavedRulesReading.Damaged("A sub-category has a blank name or one its sibling has.");
            siblings.Add(new Subcategory(new SubcategoryId(saved.Id), saved.Name));
        }

        var catalog = new CategoryCatalog(builtIn.Categories
            .Select(c => added.TryGetValue(c.Id, out var subs) ? c with { Subcategories = [.. c.Subcategories, .. subs] } : c)
            .Concat(userCategories));

        // The rules.
        var rules = new List<Rule>();
        var orders = new HashSet<long>();
        foreach (var saved in file.Rules)
        {
            if (!Fresh(saved.Id))
                return SavedRulesReading.Damaged("A rule has an empty, built-in-like or repeated identifier.");
            if (saved.Priority < 1 || saved.Order < 1 || !orders.Add(saved.Order))
                return SavedRulesReading.Damaged("A rule has a priority or creation order below 1, or a repeated order.");
            if (Load(saved.Condition) is not { } condition || condition.Validate() is not null)
                return SavedRulesReading.Damaged("A rule's condition is unknown, incomplete or has fields of another kind.");
            if (catalog.FindSubcategory(new SubcategoryId(saved.Target)) is null)
                return IsUser(saved.Target)
                    ? SavedRulesReading.Damaged("A rule targets a user sub-category that isn't there.")
                    : SavedRulesReading.Newer("A rule targets a built-in sub-category this DocuSpend doesn't have.");
            rules.Add(new Rule(new RuleId(saved.Id), RuleSource.User, saved.Priority, saved.Order, condition, new SubcategoryId(saved.Target)));
        }

        try
        {
            return new SavedRulesReading(SavedRulesStatus.Loaded, CategorizationConfig.Create(catalog, defaults.DefaultRules, rules), null);
        }
        catch (ArgumentException e)
        {
            // The checks above should catch everything Create checks; this keeps a gap between them from crashing.
            return SavedRulesReading.Damaged(e.Message);
        }
    }

    private static SavedCondition Save(Condition condition) => condition switch
    {
        TypeCondition c => new SavedCondition("type", Types: c.Types.Select(Words.Type).ToList()),
        TextCondition c => new SavedCondition("text", Text: c.Text, Mode: Words.Mode(c.Mode)),
        CounterpartyNameCondition c => new SavedCondition("counterpartyName", Text: c.Text, Mode: Words.Mode(c.Mode)),
        CounterpartyAccountCondition c => new SavedCondition("counterpartyAccount", Account: c.Account),
        AmountCondition c => new SavedCondition("amount", Min: c.Min, Max: c.Max),
        VariableSymbolCondition c => new SavedCondition("variableSymbol", Symbol: c.Symbol),
        _ => throw new NotSupportedException($"No saved shape for {condition.GetType().Name}."),
    };

    /// <summary>The condition, or null when its kind is unknown, a value is unknown, or it has a field of another kind.</summary>
    private static Condition? Load(SavedCondition c)
    {
        bool Only(bool types = false, bool text = false, bool mode = false, bool account = false, bool amount = false, bool symbol = false) =>
            (c.Types is not null) == types && (c.Text is not null) == text && (c.Mode is not null) == mode
            && (c.Account is not null) == account && (amount || c.Min is null && c.Max is null) && (c.Symbol is not null) == symbol;

        switch (c.Kind)
        {
            case "type" when Only(types: true):
                var types = c.Types!.Select(Words.ParseType).ToList();
                return types.Any(t => t is null) || types.Distinct().Count() != types.Count ? null : new TypeCondition(types.Select(t => t!.Value));
            case "text" when Only(text: true, mode: true) && Words.ParseMode(c.Mode!) is { } mode:
                return new TextCondition(c.Text!, mode);
            case "counterpartyName" when Only(text: true, mode: true) && Words.ParseMode(c.Mode!) is { } mode:
                return new CounterpartyNameCondition(c.Text!, mode);
            case "counterpartyAccount" when Only(account: true):
                return new CounterpartyAccountCondition(c.Account!);
            case "amount" when Only(amount: true):
                return new AmountCondition(c.Min, c.Max);
            case "variableSymbol" when Only(symbol: true):
                return new VariableSymbolCondition(c.Symbol!);
            default:
                return null;
        }
    }

    private static bool IsUser(string id) => id.StartsWith(UserPrefix, StringComparison.Ordinal);

    private static bool IsBlank(string name) => TextMatching.IsBlank(name);

    private static bool SameName(string a, string b) => TextMatching.Fold(a).Trim() == TextMatching.Fold(b).Trim();
}

/// <summary>
/// The words the saved text uses for values. They are fixed: renaming something in the code never changes them, so
/// saved rules keep reading. A value missing here fails a test.
/// </summary>
internal static class Words
{
    private static readonly (CategoryRole Value, string Word)[] Roles =
        [(CategoryRole.Spending, "spending"), (CategoryRole.Income, "income"), (CategoryRole.Excluded, "excluded")];

    private static readonly (TextMode Value, string Word)[] Modes =
        [(TextMode.Contains, "contains"), (TextMode.EqualTo, "equals")];

    private static readonly (TransactionType Value, string Word)[] Types =
    [
        (TransactionType.CardPayment, "cardPayment"),
        (TransactionType.WalletCardPayment, "walletCardPayment"),
        (TransactionType.Transfer, "transfer"),
        (TransactionType.InstantTransfer, "instantTransfer"),
        (TransactionType.AccountFee, "accountFee"),
        (TransactionType.FeeReversal, "feeReversal"),
    ];

    public static string Role(CategoryRole value) => Roles.Single(r => r.Value == value).Word;

    public static CategoryRole? ParseRole(string word) => Roles.Where(r => r.Word == word).Select(r => (CategoryRole?)r.Value).FirstOrDefault();

    public static string Mode(TextMode value) => Modes.Single(m => m.Value == value).Word;

    public static TextMode? ParseMode(string word) => Modes.Where(m => m.Word == word).Select(m => (TextMode?)m.Value).FirstOrDefault();

    public static string Type(TransactionType value) => Types.Single(t => t.Value == value).Word;

    public static TransactionType? ParseType(string word) => Types.Where(t => t.Word == word).Select(t => (TransactionType?)t.Value).FirstOrDefault();
}
